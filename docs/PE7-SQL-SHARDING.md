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

GET /api/admin/analytics/* (AnalyticsService)
  → fan-out all shards; legacy DB excludes orders already in order_shard_routes (no double-count)

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
2. **Backfill:** copy legacy orders into target shards by `UserId` hash; register `order_shard_routes`; optional delete from legacy after verify.

### Backfill CLI

Dry-run (default):

```bash
cd backend
export ConnectionStrings__DefaultConnection="Host=...;Database=novacart_commerce;..."
export ConnectionStrings__CommerceShard0="...Database=novacart_commerce_0;..."
export ConnectionStrings__CommerceShard1="...Database=novacart_commerce_1;..."
export OrderSharding__Enabled=true
export OrderSharding__ShardCount=2

./scripts/backfill-order-shards.sh          # dry-run
./scripts/backfill-order-shards.sh --apply  # write shards + routes
./scripts/backfill-order-shards.sh --apply --delete-legacy  # also remove legacy rows
```

Implementation: `OrderShardBackfillService` in `Novacart.Core/Infrastructure/Sharding/`.

### Analytics cross-shard

When `OrderSharding.Enabled=true`, `AnalyticsService` fans out summary / sales-over-time / best-sellers across all shards, then adds legacy DB totals **excluding** orders that already have a route row (prevents double-count after backfill).

## Integration tests

| Test | Scope |
|------|-------|
| `OrderShardingIntegrationTests` | In-memory multi-DB harness — cross-shard analytics + backfill dry-run/apply |
| `OrderShardResolverTests` | FNV-1a shard index stability |

Staging: enable sharding on `order-api` in compose, place test orders on both shards, verify admin analytics totals match expected fan-out sum.

## Boundaries

- Auth / Product / Cart DBs unchanged.
- Not PostgreSQL native partitioning — application-level routing (matches PE-1 service style).
- Admin cross-shard pagination is approximate at very large scale (in-memory merge).
- Monolith / `order-api` hosts sharded analytics; `product-api` uses product DB only (analytics over orders requires commerce connection in microservice layout — see HANDOFF).

## Related

- [TODO.md § PE-7](../TODO.md#pe-7--sql-sharding)
- [docs/database-standards.md](database-standards.md)
- `Novacart.Core/Infrastructure/Sharding/`
- `backend/scripts/OrderShardBackfill/` — CLI project
