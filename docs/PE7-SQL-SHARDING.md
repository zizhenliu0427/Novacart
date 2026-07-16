# PE-7 — Order SQL sharding (UserId hash pilot)

> **Status:** Implemented (2026-07-16). **Disabled by default** — set `OrderSharding__Enabled=true` to activate.

## Design

| Component | Role |
|-----------|------|
| **Shard key** | `UserId` — FNV-1a hash mod `ShardCount` |
| **Shard DBs** | `novacart_commerce_0`, `novacart_commerce_1` (pilot: 2 shards) |
| **Routing DB** | `novacart_commerce` (DefaultConnection) — `order_shard_routes` table |
| **Co-located** | `orders`, `order_items`, `order_status_history`, `payments` per shard |

PostgreSQL remains the source of truth. Each shard runs the **same EF migration** as legacy commerce DB.

## Flow

```
Checkout (PaymentService)
  → shard = hash(userId) % ShardCount
  → INSERT order + items on CommerceShard{N}
  → INSERT order_shard_routes on DefaultConnection

GET /api/orders (OrderService)
  → single shard via userId (no fan-out)

GET /api/admin/orders (AdminOrderService)
  → fan-out all shards + legacy DB, merge in memory, paginate

Webhook / saga (orderId only)
  → lookup order_shard_routes → CommerceShard{N}
  → fallback DefaultConnection for pre-sharding orders
```

## Configuration (order-api)

```json
"OrderSharding": {
  "Enabled": false,
  "ShardCount": 2
},
"ConnectionStrings": {
  "DefaultConnection": "...Database=novacart_commerce...",
  "CommerceShard0": "...Database=novacart_commerce_0...",
  "CommerceShard1": "...Database=novacart_commerce_1..."
}
```

Enable in Docker:

```yaml
OrderSharding__Enabled: "true"
OrderSharding__ShardCount: "2"
ConnectionStrings__CommerceShard0: Host=postgres;...;Database=novacart_commerce_0;...
ConnectionStrings__CommerceShard1: Host=postgres;...;Database=novacart_commerce_1;...
```

Run migrations on all commerce DBs: `backend/scripts/migrate-databases.sh`.

## Migration plan (existing data)

1. **Pilot:** new orders only — routed to shards; legacy rows stay on `novacart_commerce`.
2. **Backfill (manual):** per-user `INSERT … SELECT` into target shard + route row; verify counts.
3. **Analytics:** `AnalyticsService` still reads DefaultConnection only — fan-out or OLAP replica is follow-up.

## Boundaries

- Auth / Product / Cart DBs unchanged.
- Not PostgreSQL native partitioning — application-level routing (matches PE-1 service style).
- Admin cross-shard pagination is approximate at very large scale (in-memory merge).

## Related

- [TODO.md § PE-7](../TODO.md#pe-7--sql-sharding)
- [docs/database-standards.md](database-standards.md)
- `Novacart.Core/Infrastructure/Sharding/`
