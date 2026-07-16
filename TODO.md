# Novacart — TODO (Planned Enhancements)

> **Scope:** Future scaling features from [README §Planned Enhancements](README.md#planned-enhancements).
> Detailed context, triggers, and dependency notes: [HANDOFF.md §11](HANDOFF.md#11-planned-enhancements-scaling-tail--not-scheduled).
>
> **P1, P2, P3, and P14 are complete.** Nothing in this file is required for the current release.
> Legend: `[ ]` not started · `[~]` in progress · `[x]` done

---

## PE-1 — Microservices (final architecture)

**Canonical design:** [docs/MICROSERVICES-PE1.md](docs/MICROSERVICES-PE1.md) · [中文版](docs/MICROSERVICES-PE1_ZH.md)

**Stack:** .NET **Aspire** + **YARP** + **Polly** + **MassTransit** + **RabbitMQ** + **Transactional Outbox** + **MassTransit Saga**. Services: Auth, Product, Cart, Order (database-per-service target). **PE-2** and **PE-5** are implemented **inside** this rollout, not as separate tracks.

**Trigger:** Multiple teams need independent deploys, uneven scaling across domains, or in-process `EmailQueue` is insufficient across replicas.

**Phase status (2026-07-16):** **PE-1 sealed** — microservices default stack; logical 4-DB isolation documented; Stripe refund compensation; saga harness + E2E smoke script.

- [x] Document final topology and Spring Cloud comparison
- [x] Add **Aspire AppHost** + **YARP gateway** + **RabbitMQ** to docker-compose / AppHost
- [x] Identify service boundaries (Auth / Product / Cart / Order); **4 DBs** (`novacart_auth`, `novacart_product`, `novacart_commerce`, `novacart_cart`) in Docker
- [x] **Order service:** MassTransit EF **Outbox** + `PaymentCompleted` (distributed webhook path)
- [x] Implement **OrderCheckoutSaga** — **`OrderCheckoutStateMachine`** (EF persistence) replaces `StockReserved` / `StockReservationFailed` consumers
- [x] Extract **Product** service + **`ReserveStockConsumer`** + **Redlock (PE-4)** on `StockReservationService`
- [x] Extract **Cart** service + **`ClearCartForOrder` consumer**
- [x] Extract **Auth** service
- [x] Extract **Order** service (webhook, admin orders, saga host)
- [x] Replace in-process **`EmailQueue`** with MassTransit **`SendOrderConfirmationConsumer`** + **`SendEmailConsumer`** (Order microservice; monolith retains `EmailBackgroundWorker` for legacy compose)
- [x] **Polly** via ServiceDefaults; **Refit** catalog client + Aspire/K8s service discovery
- [x] K8s deploy manifests (`k8s/microservices.yaml`, `k8s/ingress.yaml`, `k8s/secrets.example.yaml`, migrate Job, probes)
- [x] DLQ alerting + OpenTelemetry (Jaeger OTLP `:16686`)
- [x] **Phase 6:** Cart DB isolation; MassTransit retry; `GET /api/admin/system/messaging`
- [x] **Phase 7:** MassTransit email queue; Testcontainers; admin DLQ UI
- [x] **Phase 8:** Refit `IProductCatalogApi`; AppHost 4 DB + Jaeger; `docker-compose.prod.yml` 4 DB; gateway route tests
- [x] **PE-1 seal:** Stripe refund compensation; `CheckoutSagaIntegrationTests`; `scripts/e2e-microservices-smoke.sh`; [DATABASE-PER-SERVICE.md](docs/DATABASE-PER-SERVICE.md)
- [x] Split Docker Compose / prod into multi-service (+ Redis, RabbitMQ); default `docker-compose.yml` = microservices

---

## PE-2 — RabbitMQ

> **Status:** Folded into **[PE-1 final architecture](#pe-1--microservices-final-architecture)**. Implement MassTransit + RabbitMQ as part of PE-1, not as a standalone milestone.

**Purpose:** Async order processing, inventory updates, email notifications (durable, multi-instance).

**Trigger:** (See PE-1.)

- [x] *(Track under PE-1)* Add RabbitMQ to compose / Aspire AppHost
- [x] *(Track under PE-1)* MassTransit publishers/consumers, idempotency, DLQ
- [x] *(Track under PE-1)* Replace `EmailQueue` with MassTransit consumer (Order service)

---

## PE-2 legacy checklist (reference only)

<details>
<summary>Original PE-2 sub-tasks (all under PE-1 now)</summary>

- [x] Add RabbitMQ to `docker-compose.yml` (and prod compose / managed broker)
- [x] Define exchanges/queues: checkout events via MassTransit conventions
- [x] Replace or complement `EmailQueue` with RabbitMQ publisher + consumer worker(s)
- [x] Publish inventory-adjust events after payment webhook (Product consumer)
- [x] Idempotent consumers (dedupe by order ID / pending status checks)
- [x] Dead-letter queue + alerting on poison messages
- [x] Integration tests with Testcontainers (RabbitMQ management probe; MassTransit email harness)

</details>

---

## PE-3 — ElasticSearch

**Purpose:** Full-text search for product catalogues (material, style, price).

**Trigger:** Postgres ILIKE + tag/category facets fail on relevance, language, or latency at scale.

**Status (2026-07-16):** Implemented on Product API — multi-match search with Postgres fallback.

- [x] Add ElasticSearch (or OpenSearch) to dev/prod stack
- [x] Index pipeline: product create/update/delete → ES document sync (direct + startup reindex)
- [x] Search API: multi-match, filters (category, price range, tags, metadata fields)
- [x] Frontend: wire `/products` search to ES-backed endpoint (keep URL-driven facets)
- [x] Fallback to Postgres search if ES unavailable
- [x] Benchmark vs current `ProductService` ILIKE path — see [docs/PE3-ELASTICSEARCH.md](docs/PE3-ELASTICSEARCH.md)

---

## PE-4 — Distributed Lock & inventory hardening (Redis)

> **Status:** **Complete** (baseline + production hardening, 2026-07-16). **PE-6 (Redis cart)** remains separate.

**Purpose:** Prevent oversell and protect hot inventory paths when Product (and checkout) run at multiple replicas.

**Trigger:** PE-1 horizontal scaling; flash-sale or checkout burst; oversell risk beyond single-node Redis.

**Implemented:** Redlock + checkout holds (TTL) + atomic SQL decrement + YARP rate limiting + Redis HA config + OTel stock metrics. See [docs/PE4-PRODUCTION-HARDENING.md](docs/PE4-PRODUCTION-HARDENING.md).

### Done (baseline)

- [x] *(PE-1)* Redlock in Product `ReserveStockConsumer` / `StockReservationService`
- [x] Lock key per `ProductId` with short TTL (`novacart:stock:lock:{productId}`, 30s expiry)
- [x] Load test concurrent checkout on low-stock SKU (`StockReservationConcurrencyTests` + Docker Redis)
- [x] Document lock semantics in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)

### Production hardening

- [x] **Stock reservation (预占库存):** hold at checkout / Stripe session creation; TTL expiry worker; release on session expired / stock failure
- [x] **DB atomic decrement:** conditional `UPDATE … WHERE stock_quantity >= @q` in `ProductStockRepository`
- [x] **Rate limiting / queuing:** YARP fixed-window limiter on `/api/checkout` (+ queue); webhook excluded
- [x] **Redis HA:** `Redis:Configuration` connection string + [docs/PE4-REDIS-HA.md](docs/PE4-REDIS-HA.md) (Sentinel / Cluster)
- [x] **Observability:** `Novacart.Stock` OTel meter (lock wait, holds, atomic decrement, lock failures)

### Spring Cloud — large e-commerce inventory stack (reference)

Typical **Java / Spring Cloud** mall architecture maps roughly as follows (Novacart .NET equivalents in parentheses):

| Capability | Spring Cloud / ecosystem (typical) | Novacart today | Target (PE-4+) |
|---|---|---|---|
| Service split | Spring Boot microservices | ✅ Auth / Product / Cart / Order | — |
| API gateway | Spring Cloud Gateway | ✅ YARP | Rate limit (PE-4) |
| Discovery | Nacos / Eureka | ✅ Aspire / K8s Services | — |
| Sync resilience | OpenFeign + Sentinel / Resilience4j | ✅ Polly + Refit | Rate limit (PE-4) |
| Async checkout | Spring Cloud Stream + RabbitMQ | ✅ MassTransit + Saga | — |
| Outbox / reliability | Transactional outbox pattern | ✅ MassTransit EF Outbox | — |
| **Inventory lock** | Redisson / Redis lock | ✅ `RedisDistributedLockService` | HA Redis (PE-4) |
| **Inventory reservation** | Custom + Redis / DB hold table | ❌ Deduct on payment webhook only | Reservation + TTL (PE-4) |
| **Atomic stock SQL** | MyBatis `UPDATE … WHERE stock >=` | ⚠️ EF read-check-write under lock | Conditional UPDATE (PE-4) |
| Flash sale | Sentinel limit + MQ削峰 + 库存预热 | ❌ | Gateway queue (PE-4) |
| Search | Elasticsearch | ✅ PE-3 | — |
| Cart cache | Redis cart | Postgres cart | PE-6 (separate) |
| Config / secrets | Nacos / Spring Cloud Config | env / K8s secrets | — |
| Tracing | Sleuth / Micrometer + Zipkin | ✅ OpenTelemetry + Jaeger | PE-4 stock metrics |
| Seata (distributed TX) | 2PC across services | ❌ Saga + outbox instead | Not planned (Saga preferred) |

**Takeaway:** Spring malls rarely rely on **only** a Redis lock — they combine **reservation**, **DB conditional update**, **MQ + Saga**, **gateway限流**, and **Redis HA**. Novacart baseline (PE-1 + PE-4 lock + Saga) matches mid-tier; PE-4 hardening closes the gap to common production practice.

---

## PE-5 — Async Order Processing

> **Status:** **Complete** — core flow via PE-1 MassTransit Saga + Outbox; admin saga/DLQ retry UI added (2026-07-16).

**Purpose:** Decouple checkout (payment → inventory → email → clear cart).

**Trigger:** (See PE-1.)

- [x] *(Track under PE-1)* `OrderCheckoutSaga` / consumer choreography
- [x] *(Track under PE-1)* Compensating actions on stock failure (`StockReservationFailed`)
- [x] *(Track under PE-1)* Idempotency at each stage (`orderId`, pending status checks)
- [x] Admin visibility: failed saga / DLQ retry UI

---

## PE-5 legacy checklist (reference only)

<details>
<summary>Original PE-5 sub-tasks (all under PE-1 now)</summary>

- [x] Define saga/choreography: `PaymentCompleted` → `ReserveInventory` → `SendConfirmation` → `ClearCart`
- [x] Implement consumers for each step with compensating actions on failure
- [x] Ensure idempotency at each stage (order ID as correlation key)
- [x] Admin visibility: failed step retry or manual intervention UI

</details>

---

## PE-6 — Cart Optimisation (Redis-backed)

> **Status:** **Complete** (2026-07-16). See [docs/PE6-REDIS-CART.md](docs/PE6-REDIS-CART.md).

**Purpose:** Sub-ms cart reads, cross-device sync, guest-to-user merge.

**Trigger:** PostgreSQL cart path becomes hot or cross-device latency is unacceptable.

- [x] Cart storage model in Redis (JSON snapshot per user/session key)
- [x] `CartService` read-through / write-through Redis with Postgres source of truth
- [x] Preserve guest `SessionId` → `UserId` merge semantics from current P2 implementation
- [x] TTL/eviction policy for abandoned guest carts (30d guest / 90d user)
- [x] Invalidate on checkout completion (`ClearCartConsumer` → `ClearCartAsync`)

---

## PE-7 — SQL Sharding

> **Status:** **Complete** (2026-07-16 pilot). See [docs/PE7-SQL-SHARDING.md](docs/PE7-SQL-SHARDING.md). **Disabled by default.**

**Purpose:** Horizontal partitioning of large tables by date or user ID.

**Trigger:** Orders / order_status_history / payment_webhooks tables exceed single-node Postgres capacity.

- [x] Choose shard key (e.g. `UserId` hash or `CreatedAt` time range) — **UserId FNV-1a hash**
- [x] Pilot shard on `orders` + `order_items` (highest growth) — co-locate payments + status history
- [x] Routing layer in EF or raw SQL (shard resolver in service layer) — `IShardedOrderDb` + `order_shard_routes`
- [x] Migration plan for existing data; cross-shard admin queries (analytics) strategy — documented in PE7 doc
- [x] Update [docs/database-standards.md](docs/database-standards.md)

---

## PE-8 — Thread Pool Tuning

**Purpose:** Custom thread pool for flash sales and bulk order processing.

**Trigger:** Thread-pool starvation or tail latency under burst checkout / webhook load.

- [ ] Profile checkout + webhook under load (dotnet-counters, Application Insights)
- [ ] Configure `ThreadPool` min threads or dedicated `TaskScheduler` for hot paths
- [ ] Optional: isolate webhook processing to dedicated worker process
- [ ] Document tuned values per environment in deployment guide

---

## PE-9 — AI Chatbot (Low Priority)

**Purpose:** Customer service bot via OpenAI API or Ollama (local LLM).

**Trigger:** Product requirement for automated support (order status, shipping, returns FAQ).

- [ ] Choose provider: OpenAI API vs self-hosted Ollama
- [ ] Backend proxy endpoint (never expose API keys to browser)
- [ ] Context injection: user's recent orders, product catalogue snippets (RAG optional)
- [ ] Frontend chat widget (floating panel, rate limit, fallback to static FAQ)
- [ ] Privacy: PII redaction, opt-in, logging policy

---

## PE-10 — Internationalisation (i18n) — ✅

**Purpose:** Bilingual UI (Chinese/English) with URL-based language routing.

**Trigger:** Product requirement for multi-locale storefront.

- [x] Adopt `next-intl` with `/en/` and `/zh/` App Router segments (`localePrefix: always`).
- [x] Message catalogues: `messages/en.json` (Australian English, en-AU spelling) + `messages/zh.json` (Simplified Chinese).
- [x] `LocaleSwitcher` in header; locale-aware `Link`/`useRouter` via `@/i18n/navigation`.
- [x] Customer shell translated: home, auth, nav, footer, admin nav, ProductCard, offline, not-found.
- [x] `formatPrice` uses `en-AU` / `zh-CN` Intl locales; currency remains AUD.
- [x] Middleware combines i18n routing with existing auth guards.
- [ ] Translate remaining admin page body copy (products/orders/pricing forms — optional follow-up).
- [ ] `hreflang` SEO tags (optional follow-up).

---

## Completed (do not re-open)

Everything below is **done** — tracked in [HANDOFF.md](HANDOFF.md) §5–§10:

- [x] P1 MVP (auth, products, cart, Stripe checkout, orders)
- [x] P2 P14 features (RBAC, admin, pricing, wishlist, guest cart, email, PWA, Square, analytics, advanced search)
- [x] P3 engineering (CI/CD, mappers/factories, UI primitives, compression/cache, deployment docs)
- [x] P14 preferred (refresh tokens, async email queue, S3/LocalStack)
- [x] P14 documentation (ARCHITECTURE, UI-DESIGN, USER-GUIDE, DEMO — EN + ZH)
