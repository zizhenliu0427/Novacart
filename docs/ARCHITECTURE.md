# Novacart — Architecture

> System architecture, layering, data flows, and design decisions.
> Companion to the [ER Diagram](../Database_ER_Diagram.md), [DB Standards](database-standards.md), and [Deployment Guide](deployment-guide.md).

---

## 1. Overview

Novacart is a full-stack e-commerce platform built on a **MonolithFirst** philosophy: a single, well-layered application serves as the foundation, with scaling extensions (microservices, message queues, full-text search) deferred to a later phase when traffic demands it (see [README §Planned Enhancements](../README.md#planned-enhancements)).

The system serves three user roles — **Customer**, **Administrator**, and **System Administrator** — covering browsing, cart, checkout, payments, order management, dynamic pricing, analytics, and PWA offline support.

```
┌─────────────┐   HTTPS    ┌──────────────────┐   REST API    ┌────────────────────┐
│   Browser   │ ──────────▶│   Next.js 14      │ ────────────▶│  ASP.NET Core 8     │
│  (Client /  │ ◀──────────│   Frontend        │ ◀────────────│  Backend API        │
│   PWA)      │            │   (App Router)    │               │  (REST + JWT cookie)│
└─────────────┘            └──────────────────┘               └──────────┬───────────┘
                                                                            │
                                              ┌────────────────┬───────────┼───────────────┐
                                              ▼                ▼           ▼               ▼
                                        ┌──────────┐    ┌──────────┐  ┌──────────┐   ┌──────────┐
                                        │PostgreSQL│    │  Redis   │  │  Stripe  │   │  Square  │
                                        │ (source  │    │ (cache + │  │ (payment │   │ (catalogue│
                                        │  of truth│    │  session)│  │  webhook)│   │   sync)  │
                                        └──────────┘    └──────────┘  └──────────┘   └──────────┘
```

### Tech stack at a glance

| Layer | Technology |
|---|---|
| Frontend | Next.js 14 (App Router), TypeScript, Tailwind CSS, ECharts |
| Backend | ASP.NET Core 8 (C#), RESTful APIs |
| ORM | Entity Framework Core (Code-First, Npgsql provider) |
| Database | PostgreSQL 16 (source of truth) |
| Cache | Redis 7 (cache-aside for products & orders) |
| Payment | Stripe (Checkout Sessions + signed webhooks, tokenised — no card storage) |
| Catalog integration | Square Catalogue API (sandbox sync) |
| Auth | JWT (HS256) in HttpOnly cookie + bcrypt password hashing |
| Email | MailKit (SMTP) |
| Container | Docker, Docker Compose |
| Cloud target | AWS (EC2 / RDS / ElastiCache / S3) — see [Deployment Guide](deployment-guide.md) |

---

## 2. Backend layering

The backend follows a strict **`Controller → Service → Mapper → Entity`** layering. Each layer has a single responsibility, enforced by convention:

```
┌─────────────────────────────────────────────────────────────┐
│  Controllers (thin)                                         │
│  Parse request · call a service · return DTO                │
│  No business logic · no EF queries · no try/catch           │
└──────────────────────┬──────────────────────────────────────┘
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  Factories & Strategies                                     │
│  OrderFactory (cart → order aggregate)                      │
│  PaymentStrategyFactory (resolve gateway by code)           │
│  IPaymentStrategy (StripePaymentStrategy)                   │
└──────────────────────┬──────────────────────────────────────┘
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  Services (all business logic + EF access)                  │
│  Interface + impl, registered in DI                         │
└──────────────────────┬──────────────────────────────────────┘
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  Mappers (static, entity → DTO projection)                  │
│  ProductMapper · OrderMapper                                │
└──────────────────────┬──────────────────────────────────────┘
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  Entities + AppDbContext (EF Core model)                    │
│  Never returned directly from controllers                   │
└─────────────────────────────────────────────────────────────┘
```

**Layer rules (established conventions):**

- **Controllers** are thin: parse the request, call a service, return a DTO. They contain **no business logic, no EF queries, and no `try/catch`**. Unhandled exceptions are caught by the global exception handler and mapped to `ProblemDetails`.
- **Services** hold all business logic and EF Core access. Each is an interface + implementation pair (e.g. `IAuthService` / `AuthService`), registered in the DI container.
- **Mappers** are static classes that project entities to DTOs (`ProductMapper`, `OrderMapper`), completing the `Controller → Service → Mapper → Entity` chain.
- **DTOs** live in `backend/Models/Dtos/**`. EF entities are **never** returned directly from controllers.

### Service catalogue

| Service | Responsibility |
|---|---|
| `AuthService` | Register / login; bcrypt hashing; JWT issuance |
| `JwtTokenService` | HS256 token signing (Singleton, stateless) |
| `ProductService` | Catalogue list/detail; search, facets, sort, pagination; Redis cache; dynamic pricing |
| `AdminProductService` | Product CRUD; slug validation; metadata validation; soft-delete; cache invalidation |
| `CartService` | Auth + guest cart CRUD; merge-on-login; applies dynamic pricing |
| `OrderService` | User order history/detail (read); Redis cache; ownership checks |
| `PaymentService` | Checkout orchestration; Stripe webhook handling (idempotent, transactional) |
| `AdminOrderService` | Admin order view + 6-state status machine + audit history + email |
| `PricingService` | Pure rule-evaluation engine (percent/flat/fixed, scope priority, time windows) |
| `PriceRuleService` | Pricing rule CRUD + validation |
| `UserService` | Customer profile read/update |
| `WishlistService` | Wishlist add/remove/get (idempotent, unique-constraint dedup) |
| `AddressService` | Shipping/billing address CRUD; default-address uniqueness enforcement |
| `AnalyticsService` | Sales aggregation (totals, sales-over-time, best-sellers, low-stock) |
| `SquareCatalogueService` | Catalogue sync from Square API (sandbox, with simulation fallback) |
| `EmailService` | MailKit SMTP sending (or console-log fallback when unconfigured) |
| `RedisCacheService` | Generic Get/Set/Remove/RemoveByPrefix (JSON serialisation) |
| `GlobalExceptionHandler` | Maps `AppException`/`AuthException` → `ProblemDetails` |

### Design patterns

| Pattern | Where | Purpose |
|---|---|---|
| **Strategy** | `IPaymentStrategy` → `StripePaymentStrategy`; `PaymentStrategyFactory` | Swappable payment gateways. Adding a provider = implement the interface + register in DI. |
| **Factory** | `OrderFactory.CreateFromCart(...)` | Isolates order-aggregate construction (order + items + price snapshot + address snapshot) from `PaymentService`. |
| **Gateway** | `ISquareCatalogueGateway` wrapping the Square SDK | Makes the third-party client mockable for tests. |
| **Cache-aside** | `IRedisCacheService` used by `ProductService`/`OrderService` | Read-through cache with prefix-based invalidation on writes. |
| **Global exception handler** | `IExceptionHandler` (`GlobalExceptionHandler`) | Single point that maps domain exceptions to HTTP `ProblemDetails`; controllers stay clean. |

---

## 3. Error handling

Domain exceptions carry an HTTP status code and are translated uniformly by the global handler — **controllers never contain `try/catch`**.

```
Service throws                          GlobalExceptionHandler            HTTP response
─────────────────                       ──────────────────────           ──────────────
AppException(message, statusCode)  ───▶ maps to ProblemDetails      ───▶ { status, title, detail, instance }
AppException.NotFound()            ───▶ 404
AppException.Conflict()            ───▶ 409
AppException.Forbidden()           ───▶ 403
AuthException(message, statusCode) ───▶ its status (401/403)
UnauthorizedAccessException        ───▶ 401   (thrown by GetUserId() helpers)
(any other)                        ───▶ 500   (logged at Error level)
```

Model-validation failures (e.g. `[Required]`, `[Url]`) are shaped by `ConfigureApiBehaviorOptions` into an RFC 7807 `ValidationProblemDetails` (400) with an `errors` dictionary.

---

## 4. Cross-cutting infrastructure

### DI registrations (`Program.cs`)

- **Singletons** (stateless / thread-safe): `AppDbContext`, `IConnectionMultiplexer` (Redis), `IRedisCacheService`, `IJwtTokenService`.
- **Scoped** (per-request): all business services, factories, strategies.
- **Hosted services**: none yet (email is currently synchronous — async-queue is a planned enhancement).

### Middleware pipeline (order matters)

```
UseExceptionHandler()          ← first; catches everything downstream
  └─ UseResponseCompression() / UseResponseCaching()   ← Brotli/Gzip + HTTP cache headers
      └─ UseSwagger() / UseSwaggerUI()                 ← Dev only
          └─ UseCors("AllowFrontend")                  ← credentials allowed for cookie auth
              └─ UseAuthentication()                   ← JWT bearer (reads HttpOnly cookie)
                  └─ UseAuthorization()                ← role-based [Authorize]
                      └─ MapControllers() + /api/health
```

### Security

- **JWT in HttpOnly cookie** (`novacart_jwt`, `Secure`, `SameSite=Strict`, `Path=/api`): the frontend never touches the raw token — protects against XSS. The bearer middleware reads it from the cookie via `OnMessageReceived`, with a Bearer-header fallback for Swagger.
- **Edge flag cookie** (`novacart_authed=1`): a non-sensitive flag the Next.js Edge middleware reads to guard protected routes (since it can't access the HttpOnly JWT from `localStorage`).
- **RBAC**: 3 roles (`customer` / `admin` / `sysadmin`) carried as JWT claims. Admin endpoints use `[Authorize(Roles = RoleNames.AdminRoles)]` (`admin,sysadmin`); sensitive system operations use `[Authorize(Roles = RoleNames.SysAdmin)]` (sysadmin only).
- **Payment tokenisation**: Stripe handles card data; Novacart never stores card numbers — only Stripe's payment intent/session IDs.
- **Password hashing**: bcrypt (salted, one-way).

---

## 5. Data flow — checkout & payment (end-to-end)

This is the most critical flow; it exercises the Strategy + Factory patterns, idempotency, and transactional consistency.

### A. Create checkout session

```
Browser                Next.js                 CheckoutController            PaymentService
  │  click "checkout"    │  POST /api/checkout    │  CreateCheckout()           │  ProcessCheckoutAsync()
  │ ────────────────────▶│ ─────────────────────▶│ ──────────────────────────▶│
  │                      │                        │                            │  1. load cart (409 if empty)
  │                      │                        │                            │  2. validate stock (410/422)
  │                      │                        │                            │  3. load active price rules
  │                      │                        │                            │  4. OrderFactory.CreateFromCart()
  │                      │                        │                            │       → subtotal/shipping/tax/total
  │                      │                        │                            │       → OrderItem snapshots price
  │                      │                        │                            │  5. save Order (Pending)
  │                      │                        │                            │  6. PaymentStrategyFactory → Stripe
  │                      │                        │                            │  7. StripePaymentStrategy.CreateSession
  │                      │                        │                            │  8. record Payment (Pending)
  │                      │                        │  ← CheckoutResponse{url}   │
  │                      │  redirect to Stripe    │ ◀──────────────────────────│
  │ ◀────────────────────│ ◀──────────────────────│                             │
```

### B. Stripe webhook (payment confirmation)

```
Stripe                 CheckoutController          PaymentService.HandleWebhookAsync
  │  checkout.session.completed                       │
  │  (signed)                                         │
  │ ────────────────────────────────────────────────▶│
  │                                                   │  1. verify signature (400 if invalid)
  │                                                   │  2. idempotency: insert PaymentWebhook
  │                                                   │     (unique idx on event_id → 200 + return on dup)
  │                                                   │  3. ExecutePaymentCompletionAsync (DB TRANSACTION):
  │                                                   │       - reload order (skip if not Pending)
  │                                                   │       - re-check stock
  │                                                   │       - decrement stock
  │                                                   │       - Order → Paid, Payment → Succeeded
  │                                                   │       - clear user's cart
  │                                                   │     COMMIT
  │                                                   │  4. send confirmation email (best-effort)
  │                                                   │  5. invalidate order + product caches
  │  ◀──────── 200 OK ──────────────────────────────│
```

**Key guarantees:**
- **Idempotency**: the unique index `idx_payment_webhooks_event_id` means a replayed webhook is a no-op.
- **Atomicity**: stock decrement + status transition + cart clear happen in one DB transaction — all or nothing.
- **Frozen pricing**: `OrderItem.PriceAtPurchase` is snapshot at checkout time; later price-rule changes never affect historical orders.

---

## 6. Caching strategy

Redis is wired and actively used (cache-aside), with prefix-based invalidation.

| What | Key pattern | TTL | Invalidated by |
|---|---|---|---|
| Product list | `products:list:{filters}:p{page}` | 60s | `AdminProductService` writes; `PaymentService` webhook |
| Order list (per user) | `orders:user:{userId}:p{page}` | 30s | new order / webhook |
| Order detail | `orders:detail:{orderId}` | 30s | (TTL expiry — see known issue below) |

Cache hits on order detail still perform an **ownership check** before returning, preventing cross-user data leaks.

---

## 7. Frontend architecture

Next.js 14 **App Router** with a context-based state model.

```
RootLayout
 ├─ AuthProvider          (login/register/logout, /me rehydration)
 │   └─ CartProvider      (loads on auth, guest cart by session)
 │       └─ WishlistProvider (hydrates on auth, optimistic toggle)
 │           └─ ToastProvider (global notifications)
 │               ├─ HeaderNav      (sticky top bar, cart badge, user menu)
 │               └─ <main>         (page content)
 ├─ footer
 └─ <script> register /sw.js   (PWA service worker)
```

- **Route guarding**: Next.js Edge `middleware.ts` checks the `novacart_authed` flag cookie to protect `/cart`, `/orders`, `/checkout`, `/admin/*` and redirect when needed.
- **Admin shell**: `/admin` has its own nested `layout.tsx` with a sidebar (collapses to a hamburger below `md`/768px) and a client-side role gate.
- **API layer**: `apiCall` wrapper (`lib/api.ts`) injects `credentials: 'include'`, parses `ProblemDetails`/validation errors, distinguishes 401 (expired → redirect to login) from 403 (permission error, stay).
- **Design system**: token-driven (see [UI Design](UI-DESIGN.md)); ECharts dashboard is dynamically imported (`ssr:false`) for App Router compatibility.

---

## 8. Known technical debt (non-blocking)

These are recorded for honesty and future refactors — they do not block current functionality:

1. **Pricing-rule loading is duplicated** in `PaymentService`, `ProductService`, and `CartService` (each has its own `LoadActiveRulesAsync`). Extract into a shared helper.
2. **Index naming is inconsistent**: `OnModelCreating` uses `idx_table_col` (Alibaba style), but the `AddPerformanceIndexes` migration left EF defaults (`IX_Table_Col`). Align on one convention.
3. **Order-detail cache isn't invalidated** on webhook completion (only the list prefix is cleared); detail rows rely on 30s TTL expiry. Minor staleness window.
4. **`AnalyticsService` has no cache** injected, yet `AdminSystemController.ClearCache` already deletes an `analytics:` prefix — the invalidation hook is pre-wired but unused.
5. **Some controller XML comments are stale** (`UsersController`/`WishlistController` say "SCAFFOLD / 501" but the services are fully implemented).

---

## 9. Related documents

- [Database ER Diagram & Schema Design](../Database_ER_Diagram.md) — entity relationships, architectural schema decisions, indexing strategy
- [Database Standards](database-standards.md) — Alibaba convention audit, Guid PK rationale
- [UI Design System](UI-DESIGN.md) — design tokens, component library, responsive strategy
- [Deployment Guide](deployment-guide.md) — AWS architecture, production Docker Compose, env-var reference
- [Stripe Webhook Local Testing](STRIPE_WEBHOOK_LOCAL.md) — ngrok + Stripe CLI setup
- [P14 Project Specification](../P14_Modern_Ecommerce_Web_App.md) — original requirements this system satisfies
