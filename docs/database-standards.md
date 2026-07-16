# Novacart Database Standards — Alibaba Convention Audit

> P3-3 deliverable: audit naming, indexing, type, and SQL conventions against the
> [Alibaba Java Development Manual — MySQL Section](https://alibaba.github.io/p3c/)
> and document deviations.

---

## Naming Conventions

| Convention | Standard | Novacart Status |
|---|---|---|
| Table names: snake_case, lowercase, plural | ✅ Required | ✅ `orders`, `order_items`, `products`, `cart_items`, `price_rules`, `wishlist_items`, etc. |
| Column names: snake_case, lowercase | ✅ Required | ⚠️ PascalCase by default (EF Core convention). DB columns are `UserId`, `CreatedAt`, etc. |
| Index names: `IX_{table}_{column(s)}` | ✅ Required | ✅ All custom indexes follow `IX_` prefix convention. |
| Boolean columns: `is_xxx` or `has_xxx` | ✅ Required | ✅ `IsActive`, `IsDefaultShipping` (PascalCase mapping). |

> **Column naming deviation**: EF Core maps C# PascalCase properties to PascalCase columns
> by default. Re-mapping all columns to snake_case would require the `EFCore.NamingConventions`
> package. This is a known trade-off; the table names are already snake_case.

---

## Primary Key Convention

| Convention | Standard | Novacart Status |
|---|---|---|
| Primary key type: BIGINT, auto-increment | ✅ Required | ⚠️ **Deviation: Guid (UUID v4)** |

### Rationale for Guid PKs

1. **Distributed ID generation**: Guids can be generated client-side or across multiple
   app instances without coordination or sequence contention.
2. **Merge safety**: No ID collision risk when merging data from different environments
   (dev → staging → prod).
3. **Security**: Non-guessable IDs prevent enumeration attacks on API endpoints
   (`/api/orders/{id}`).
4. **EF Core compatibility**: `Guid.NewGuid()` in entity constructors provides simple,
   reliable ID assignment without database round-trips.

**Trade-off**: Guid PKs are larger (16 bytes vs 8 bytes BIGINT) and produce more
random B-tree distribution, which can increase index fragmentation. For Novacart's
current scale this is negligible.

---

## Decimal / Monetary Fields

| Convention | Standard | Novacart Status |
|---|---|---|
| Use `DECIMAL` for money, never `FLOAT` | ✅ Required | ✅ All prices, totals, and tax fields use `decimal` (EF → `numeric` in Postgres). |
| Precision: `DECIMAL(18,2)` minimum | ✅ Required | ✅ Default EF Core mapping: `numeric` (unlimited precision in Postgres). |

---

## Index Audit

All indexes added in migration `20260711100000_AddPerformanceIndexes`:

| Index | Table | Columns | Type | Query Justification |
|---|---|---|---|---|
| `IX_orders_UserId_CurrentStatus` | `orders` | `UserId`, `CurrentStatus` | Composite | User order list filtered by status |
| `IX_orders_CreatedAt` | `orders` | `CreatedAt` | Single | Analytics date-range aggregation |
| `IX_order_items_OrderId` | `order_items` | `OrderId` | Single | Order detail loading |
| `IX_order_items_ProductId` | `order_items` | `ProductId` | Single | Best-seller aggregation |
| `IX_products_IsActive_CategoryId` | `products` | `IsActive`, `CategoryId` | Composite | Catalogue list by category |
| `IX_products_Slug` | `products` | `Slug` | Unique | Slug-based lookups |
| `IX_wishlist_items_UserId_ProductId` | `wishlist_items` | `UserId`, `ProductId` | Unique composite | Prevent duplicate wishlist entries |
| `IX_cart_items_CartId_ProductId` | `cart_items` | `CartId`, `ProductId` | Unique composite | Prevent duplicate cart entries |
| `IX_price_rules_IsActive_StartsAt_EndsAt` | `price_rules` | `IsActive`, `StartsAt`, `EndsAt` | Composite | Active rule time-window queries |

**EF Core auto-generated indexes** (not listed above): PK indexes on all `Id` columns,
FK indexes on all navigation properties. These are created by `EnsureCreated()` / migrations.

---

## SQL Guidelines

| Convention | Standard | Novacart Status |
|---|---|---|
| No `SELECT *` in production code | ✅ Required | ✅ All queries use `.Select()` projections or explicit `.Include()`. |
| Parameterised queries | ✅ Required | ✅ EF Core LINQ generates parameterised SQL; no raw string interpolation. |
| Soft delete over hard delete | ✅ Recommended | ✅ Products use `IsActive` soft delete. Orders are never deleted. |
| Audit columns: `created_at`, `updated_at` | ✅ Required | ✅ `CreatedAt` on all entities; `UpdatedAt` on mutable entities. |

---

## Order sharding (PE-7)

| Topic | Standard | Novacart |
|---|---|---|
| Shard key | Stable hash on tenant/user id | `UserId` → FNV-1a mod `ShardCount` |
| Routing index | `order_id → shard_index` | Table `order_shard_routes` on routing DB (`novacart_commerce`) |
| Shard databases | Same schema, isolated rows | `novacart_commerce_0`, `novacart_commerce_1` (pilot) |
| Legacy reads | Fallback when route missing | Pre-sharding orders remain on `DefaultConnection` |
| Admin list | Cross-shard merge | Fan-out + in-memory sort/paginate (pilot) |
| Analytics | Cross-shard aggregate | Fan-out shards + legacy (exclude routed IDs) |
| Backfill | Legacy → shard copy | `OrderShardBackfillService` + `scripts/backfill-order-shards.sh` |

Details: [PE7-SQL-SHARDING.md](PE7-SQL-SHARDING.md).
