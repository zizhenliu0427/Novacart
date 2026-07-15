# PE-4 — Production inventory hardening

> **Status:** Implemented (2026-07-16). Complements PE-1 baseline Redlock + PE-3 search. **PE-6 (Redis cart) is out of scope.**

## Layers implemented

| Layer | Component | Notes |
|-------|-----------|-------|
| **Checkout hold (reservation)** | `StockHoldService`, `stock_holds` table, `StockHoldExpiryHostedService` | Hold created when order + Stripe Checkout Session is created; TTL default 15 min |
| **Hold release** | `PaymentService` (session expired webhook), `OrderCheckoutCompletionService` (stock failure) | Refit `IProductStockApi` from Order → Product in microservices |
| **Atomic decrement** | `ProductStockRepository.TryDecrementStockAsync` | `UPDATE products SET stock_quantity = … WHERE stock_quantity >= @q` |
| **Payment confirm** | `StockReservationService` | Confirms holds + atomic decrement on `PaymentCompleted` |
| **Gateway rate limit** | `Novacart.Gateway` `AddRateLimiter` | Fixed window on `/api/checkout` with optional queue; webhooks excluded |
| **Metrics** | `StockInventoryMetrics` (`Novacart.Stock` meter) | Exported via OpenTelemetry → Jaeger/Prometheus-compatible backends |
| **Redis HA** | `Redis:Configuration` + [PE4-REDIS-HA.md](PE4-REDIS-HA.md) | Dev仍用单节点 `redis:6379` |

## Checkout flow (microservices)

```
POST /api/checkout
  → Order: create Pending order
  → Product (Refit): POST /api/internal/stock/hold
  → Stripe Checkout Session
  → User pays
  → Webhook PaymentCompleted → ReserveStockConsumer
  → StockReservationService: lock → atomic SQL → confirm holds
```

## Configuration

```json
"StockHold": {
  "TtlMinutes": 15
},
"RateLimiting": {
  "CheckoutPermitLimit": 30,
  "CheckoutWindowSeconds": 60,
  "CheckoutQueueLimit": 10,
  "DefaultPermitLimit": 300,
  "DefaultWindowSeconds": 60
}
```

Gateway env overrides: `RateLimiting__CheckoutPermitLimit`, etc.

## Metrics (OTel)

| Metric | Type | Meaning |
|--------|------|---------|
| `stock.holds.created` | Counter | Holds placed at checkout |
| `stock.holds.released` | Counter | Manual / webhook release |
| `stock.holds.expired` | Counter | TTL worker expiry |
| `stock.holds.confirmed` | Counter | Confirmed after payment |
| `stock.lock.not_acquired` | Counter | Redis lock timeout |
| `stock.lock.wait_ms` | Histogram | Lock acquisition wait |
| `stock.decrement.success` | Counter | Atomic SQL succeeded |
| `stock.decrement.failure` | Counter | Atomic SQL failed (insufficient) |
| `stock.reservation.insufficient` | Counter | Hold or confirm rejected |

## Tests

- `StockHoldServiceTests` — hold availability, TTL expiry, atomic SQL
- `StockReservationConcurrencyTests` — 50 concurrent × 5 stock (Docker Redis)

```bash
dotnet test backend.Tests/Novacart.Api.Tests.csproj --filter "StockHold|StockReservation"
```

## Related

- [TODO.md § PE-4](../TODO.md#pe-4--distributed-lock--inventory-hardening-redis)
- [PE4-REDIS-HA.md](PE4-REDIS-HA.md)
- [TECH_NOTES_CN.md § PE-4](../TECH_NOTES_CN.md)
