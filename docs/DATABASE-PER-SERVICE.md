# Database-per-service (PE-1)

Novacart uses **four logical PostgreSQL databases** on one server (or four RDS instances in production):

| Database | Primary owner | Tables used at runtime |
|----------|---------------|------------------------|
| `novacart_auth` | Auth API | `users`, `roles`, `user_roles`, `refresh_tokens` |
| `novacart_product` | Product API | `categories`, `products`, `price_rules`, … |
| `novacart_cart` | Cart API | `carts`, `cart_items`, `wishlists`, … |
| `novacart_commerce` | Order API | `orders`, `order_items`, `payments`, outbox, saga state |

## Logical vs physical isolation

**Current implementation (Phase 5–8):** each database receives the **same EF Core migration** (full schema). Services only **read/write their own tables** at runtime; cross-service reads use **Refit** (`IProductCatalogApi`) or MassTransit events—not cross-database joins.

This is **logical database-per-service**: independent connection strings, backup boundaries, and scaling knobs, without maintaining four separate migration histories yet.

**Future hardening (optional):** split migrations per service so each database only contains its bounded-context tables.

## Cross-service data

| Need | Mechanism |
|------|-----------|
| Cart/Order → product price/stock | Refit `GET/POST /api/internal/catalog/*` on Product API |
| Checkout orchestration | MassTransit Saga + Outbox on Order DB |
| Stock decrement | `PaymentCompleted` → Product `ReserveStockConsumer` (Redlock) |

## Local dev

- Docker: `docker/postgres/init/01-create-databases.sql` + `backend/scripts/migrate-databases.sh`
- Aspire AppHost: four `AddDatabase("novacart-*", "novacart_*")` resources (Aspire resource names use hyphens; PostgreSQL database names use underscores, matching Docker)

## Production

- `docker-compose.prod.yml` + `.env.prod.example` — one connection string per service
- K8s: `novacart-db-*` secrets in `k8s/secrets.example.yaml`
