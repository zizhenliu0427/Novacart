# PE-6 — Redis-backed cart cache

> **Status:** Implemented (2026-07-16). PostgreSQL remains the **source of truth**; Redis accelerates cart **reads**.

## Design

| Layer | Role |
|-------|------|
| **PostgreSQL** | Durable cart rows (`carts`, `cart_items`) — all writes go here first |
| **Redis** | JSON snapshot per user/session — read-through cache after PG write |
| **Catalog API / DB** | Prices, stock, product names loaded on every DTO build (not cached in cart key) |

## Redis keys

| Key | TTL | Notes |
|-----|-----|-------|
| `novacart:cart:user:{userId}` | 90 days (config) | Refreshed on each mutation |
| `novacart:cart:session:{sessionId}` | 30 days (config) | Guest / abandoned cart eviction |

## Flow

```
GET /api/cart
  → Try Redis snapshot
  → Hit: load catalog pricing → CartDto (no PG cart query)
  → Miss: load PG → populate Redis → CartDto

POST/PATCH/DELETE cart
  → Update PostgreSQL
  → Write-through Redis snapshot

Login merge (guest → user)
  → PG merge (unchanged P2 semantics)
  → Delete guest Redis key; refresh user key

Checkout complete (ClearCartConsumer)
  → Clear PG + delete Redis key
```

## Configuration (`cart-api`)

```json
"CartRedis": {
  "Enabled": true,
  "GuestTtlDays": 30,
  "UserTtlDays": 90
}
```

Set `CartRedis__Enabled=false` to disable (falls back to Postgres-only reads).

## Integration tests

`CartRedisIntegrationTests` (Testcontainers Redis) verifies write-through snapshot when `CartRedis.Enabled=true`. Skips if Docker is unavailable — same pattern as `StockReservationConcurrencyTests`.

Staging: set `CartRedis__Enabled=true` on `cart-api`, add item via API, confirm `novacart:cart:user:{userId}` key in Redis and `GET /api/cart` read-through.

## Boundaries

- **PE-6 ≠ PE-4** — cart cache does not lock or reserve inventory.
- Disable Redis cart without code changes via config; existing Cart API contract unchanged.

## Related

- [TODO.md § PE-6](../TODO.md#pe-6--cart-optimisation-redis-backed)
- `Novacart.Core/Services/Cart/CartRedisStore.cs`
