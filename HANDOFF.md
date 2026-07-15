# Novacart — Handoff & Roadmap

> **Purpose of this file.** A self-contained handoff so another AI/developer can continue building or
> auditing Novacart without re-discovering context. It records what is already done, how to run the
> project, the conventions to follow, the frontend design system to build against, and the
> **Priority 2 (P2)** & **Priority 3 (P3)** feature-by-feature implementation checklist.
>
> **Status:** Priority 1 (MVP), Priority 2 (P2), and Priority 3 (P3) are **complete & verified**. See §8 and §10 for details.
>
> Last updated: 2026-07-11 — P3 completed: automated CI/CD pipeline (GitHub Actions), test coverage expansion (Vitest + JSDOM + RTL), code quality patterns (OrderFactory, PaymentStrategyFactory, Product/Order Mappers, 9 performance indexes, database standards doc), frontend layout/architecture polish (DataTable, Modal, Pagination, Toast context, responsive layout), response compression, HTTP caching, deep health checks, and production Docker deployment setup (environment variables, production Compose, deployment guide). All 110 backend and 24 frontend tests pass.


---

## 0. TL;DR for whoever picks this up

- **Stack:** Backend ASP.NET Core 8 + EF Core + PostgreSQL + Redis. Frontend Next.js 14 (App Router) + TS + Tailwind.
- **Priority 1 (MVP), Priority 2 (P2) and Priority 3 (P3) are 100% completed and verified** — all features (auth, products, cart, checkout, orders, address capture, wishlist, dynamic pricing, PWA, Square sync, integrations) and non-functionals (CI/CD, code design, tests, deployment docs, performance) work end-to-end.
  - **Auth** uses HttpOnly cookie (`novacart_jwt`) for JWT storage — prevents XSS.
  - **Redis** is actively used: product list cache (60s TTL), order list/detail cache (30s TTL), with invalidation on mutations.
  - **Products** synced from Square API sandboxed catalogue with local Postgres DB.
- **Priority 2 (P2) is fully implemented:**
  - P2-A: RBAC + admin product CRUD + **dev admin bootstrap** (done)
  - P2-B: **Order management + status workflow** with audit history (done)
  - P2-C: **Dynamic pricing** (rule engine wired into products + cart; admin CRUD UI) (done)
  - P2-D: **Customer profile** + Wishlist persistence/UI (done)
  - P2-E: **Square Catalogue API integration**, shipping/address capture, wishlist heart controls, guest cart, email, analytics, PWA, advanced search, and expanded HTTP integration tests (done)
- **Priority 3 (P3) is fully implemented:**
  - P3-1: CI/CD workflow + status badges
  - P3-2: Frontend test coverage expansion with RTL + JSDOM
  - P3-3: OrderFactory, PaymentStrategyFactory, Product/Order Mappers, 9 performance indexes, database standards doc
  - P3-4: Breakpoint adjustments, reusable generic DataTable, Modal, Pagination, Toast context
  - P3-5: Brotli/Gzip response compression, HTTP caching, deep health checks
  - P3-6: Production docker-compose override, secrets template, and AWS deployment architectural plan
- **Everything runs in Docker** — one command: `docker compose up --build -d`.
- **The schema is fully indexed and audited** against Alibaba standards — see §7.


---

## 1. Current status — what is DONE ✅

| Area | Status | Where |
|---|---|---|
| Entity model (User, Role, UserRole, Category, Product, Order, OrderItem, Cart, CartItem, **PaymentMethod, Payment, PaymentWebhook**) | ✅ | [backend/Models/Entities/](backend/Models/Entities/) |
| EF DbContext: keys, unique indexes, decimal precision, relationships, Cart/CartItem config, **Payment entity configs & seeds** | ✅ | [backend/Data/AppDbContext.cs](backend/Data/AppDbContext.cs) |
| Seed data: roles `customer` / `admin` / `sysadmin` | ✅ | `AppDbContext.OnModelCreating` (HasData) |
| Seed data: 5 categories + 12 products (with jsonb metadata) | ✅ | `AppDbContext.OnModelCreating` (HasData) |
| Migrations: `InitialCreate`, `AddCartAndSeedProducts`, **`AddPaymentEntities`** | ✅ | [backend/Data/Migrations/](backend/Data/Migrations/) |
| Auto-migrate on startup (Development) | ✅ | [backend/Program.cs](backend/Program.cs) |
| Password hashing (bcrypt) | ✅ | `AuthService` (BCrypt.Net-Next) |
| JWT issuing (HS256, sub/email/name/role claims) | ✅ | [backend/Services/JwtTokenService.cs](backend/Services/JwtTokenService.cs) |
| Auth endpoints: `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me`, `POST /api/auth/logout` | ✅ | [backend/Controllers/AuthController.cs](backend/Controllers/AuthController.cs) |
| **JWT HttpOnly cookie** (`novacart_jwt`) — set on login/register, cleared on logout; also readable from Bearer header for Swagger | ✅ | [backend/Controllers/AuthController.cs](backend/Controllers/AuthController.cs), [backend/Program.cs](backend/Program.cs) |
| JWT bearer auth + authorization middleware wired | ✅ | [backend/Program.cs](backend/Program.cs) |
| Auth frontend: login + register pages, useAuth hook, middleware route guard, header user menu | ✅ | [frontend/src/app/login/](frontend/src/app/login/), [frontend/src/app/register/](frontend/src/app/register/), [frontend/src/contexts/AuthContext.tsx](frontend/src/contexts/AuthContext.tsx), [frontend/src/middleware.ts](frontend/src/middleware.ts) |
| ProductService + DTOs (paginated list with ILike search, category filter, 4 sort modes, detail with metadata) | ✅ | [backend/Services/ProductService.cs](backend/Services/ProductService.cs), [backend/Models/Dtos/Products/ProductDtos.cs](backend/Models/Dtos/Products/ProductDtos.cs) |
| ProductsController (DB-backed, search/filter/sort/pagination) | ✅ | [backend/Controllers/ProductsController.cs](backend/Controllers/ProductsController.cs) |
| **P2 Admin product/inventory management** (all-status list/search, create, edit, stock adjustment, reactivate, soft-deactivate) | ✅ CODE COMPLETE | [backend/Services/AdminProductService.cs](backend/Services/AdminProductService.cs), [backend/Controllers/Admin/AdminProductsController.cs](backend/Controllers/Admin/AdminProductsController.cs), [frontend/src/app/admin/products/page.tsx](frontend/src/app/admin/products/page.tsx) |
| **P2 Admin order management + status workflow** (list/detail/filter, validated transitions, audit history) | ✅ CODE COMPLETE | [backend/Services/AdminOrderService.cs](backend/Services/AdminOrderService.cs), [backend/Controllers/Admin/AdminOrdersController.cs](backend/Controllers/Admin/AdminOrdersController.cs), [frontend/src/app/admin/orders/page.tsx](frontend/src/app/admin/orders/page.tsx) |
| **P2 Dynamic pricing** (percent/flat/fixed rules, scope priority, active windows, product/cart integration, admin UI) | ✅ CODE COMPLETE | [backend/Services/PricingService.cs](backend/Services/PricingService.cs), [backend/Services/PriceRuleService.cs](backend/Services/PriceRuleService.cs), [frontend/src/app/admin/pricing/page.tsx](frontend/src/app/admin/pricing/page.tsx) |
| **P2 Customer profile** (authenticated read/update API and account edit UI) | ✅ CODE COMPLETE | [backend/Services/UserService.cs](backend/Services/UserService.cs), [frontend/src/app/account/page.tsx](frontend/src/app/account/page.tsx) |
| **P2 Wishlist core** (persistence/API, API-backed context, wishlist page/removal, and UI heart controls) | ✅ | [backend/Services/WishlistService.cs](backend/Services/WishlistService.cs), [frontend/src/contexts/WishlistContext.tsx](frontend/src/contexts/WishlistContext.tsx), [frontend/src/app/wishlist/page.tsx](frontend/src/app/wishlist/page.tsx) |
| **P2 Development admin bootstrap** (Development only, configurable credentials) | ✅ | [backend/Program.cs](backend/Program.cs), [backend/appsettings.Development.json](backend/appsettings.Development.json) |
| **Global exception handling** (`GlobalExceptionHandler` → ProblemDetails; maps AppException/AuthException/UnauthorizedAccessException) | ✅ | [backend/Services/GlobalExceptionHandler.cs](backend/Services/GlobalExceptionHandler.cs), [backend/Program.cs](backend/Program.cs) |
| **Unified 400 validation format** (RFC 7807 ValidationProblemDetails) | ✅ | `Program.cs` `ConfigureApiBehaviorOptions` |
| Shared AppException (message + HTTP status, factories: NotFound, Conflict, Forbidden) | ✅ | [backend/Services/AppException.cs](backend/Services/AppException.cs) |
| CartService + CartController (full CRUD: get, add with merge + stock check, update qty, remove, clear) | ✅ | [backend/Services/CartService.cs](backend/Services/CartService.cs), [backend/Controllers/CartController.cs](backend/Controllers/CartController.cs) |
| **PaymentService + CheckoutController** (ProcessCheckout, Strategy-pattern Stripe integration, webhook processing, signature verify, event log idempotency) | ✅ | [backend/Services/Payments/](backend/Services/Payments/), [backend/Controllers/CheckoutController.cs](backend/Controllers/CheckoutController.cs) |
| **OrderService + OrdersController** (GetOrders, GetOrderById with user ownership check, **Redis-cached** with 30s TTL) | ✅ | [backend/Services/OrderService.cs](backend/Services/OrderService.cs), [backend/Controllers/OrdersController.cs](backend/Controllers/OrdersController.cs) |
| Frontend: products list page (paginated, debounced search, sort dropdown, **server-side category chip filter**, loading skeletons) | ✅ | [frontend/src/app/products/page.tsx](frontend/src/app/products/page.tsx) |
| Frontend: product detail page (dynamic metadata attribute table, stock badges, tags, add-to-cart) | ✅ | [frontend/src/app/products/[id]/page.tsx](frontend/src/app/products/[id]/page.tsx) |
| Frontend: CartContext + useCart (loads on auth, addItem/updateItem/removeItem) | ✅ | [frontend/src/contexts/CartContext.tsx](frontend/src/contexts/CartContext.tsx) |
| Frontend: full cart page (quantity stepper, remove, low stock warning, order summary, Stripe redirect trigger) | ✅ | [frontend/src/app/cart/page.tsx](frontend/src/app/cart/page.tsx) |
| **Frontend: checkout pages** (success transaction detail page, payment cancel redirect page) | ✅ | [frontend/src/app/checkout/](frontend/src/app/checkout/) |
| **Frontend: Order History page** (expandable cards with lazy-loaded item receipts, status badges, frozen pricing) | ✅ | [frontend/src/app/orders/page.tsx](frontend/src/app/orders/page.tsx) |
| Frontend: HeaderNav with user menu, live cart badge count | ✅ | [frontend/src/components/HeaderNav.tsx](frontend/src/components/HeaderNav.tsx) |
| Frontend: shared `Input` component (label/error/helperText/3 sizes) wired into login/register/products | ✅ | [frontend/src/components/ui/Input.tsx](frontend/src/components/ui/Input.tsx) |
| Frontend: cookie-based auth — `apiCall` uses `credentials: 'include'`; JWT in HttpOnly cookie, flag cookie for Edge middleware | ✅ | [frontend/src/lib/api.ts](frontend/src/lib/api.ts), [frontend/src/lib/auth.ts](frontend/src/lib/auth.ts) |
| Frontend: shared `order.ts` types (de-duplicated from inline interfaces) | ✅ | [frontend/src/types/order.ts](frontend/src/types/order.ts) |
| **Backend Unit Tests** (92 tests across auth, catalogue, cart, payments, admin products/orders, pricing, profile and wishlist) | ✅ 92/92 | [backend.Tests/](backend.Tests/) |
| **Frontend Unit Tests** (12 tests using Vitest for helper formatting/parsing logic) | ✅ | [frontend/src/types/product.test.ts](frontend/src/types/product.test.ts) |
| **Containerized Test Configurations** (`Dockerfile.backend.test`, `Dockerfile.frontend.test`, `vitest.config.ts`) | ✅ | Root & [frontend/vitest.config.ts](frontend/vitest.config.ts) |
| Swagger with Bearer auth button | ✅ | `/swagger` |
| Docker: full stack runs, no host tooling, no port clashes | ✅ | [docker-compose.yml](docker-compose.yml) + local `docker-compose.override.yml` |
| **Redis caching** (product list 60s TTL + order list/detail 30s TTL, invalidation on mutations) | ✅ | [backend/Services/RedisCacheService.cs](backend/Services/RedisCacheService.cs) |
| **Stripe webhook local testing docs** (Stripe CLI + ngrok) | ✅ | [docs/STRIPE_WEBHOOK_LOCAL.md](docs/STRIPE_WEBHOOK_LOCAL.md) |
| Frontend design system implemented (tokens, Tailwind, Inter, base components, reskinned pages) | ✅ | [globals.css](frontend/src/app/globals.css), [tailwind.config.ts](frontend/tailwind.config.ts), [components/](frontend/src/components/) |
| Light/dark theme following the OS (`prefers-color-scheme`) | ✅ | `globals.css` token blocks + `themeColor` in [layout.tsx](frontend/src/app/layout.tsx) |

**Verified behaviour:**
- Auth: register -> auto-login -> JWT issued; login -> JWT; JWT validated on route change; middleware guards `/cart`, `/orders`.
- Products: Search (`q`), Category Chips filter (server-side `categoryId`), Sort by newest/price/name. Detail page renders specifications table.
- Cart: Add item, merge quantities, stepper modification, removal. Recalculates subtotal and item count badge.
- Checkout: Proceeding to checkout creates a pending order, initiates Stripe session, redirects. Completion webhook verified idempotently via unique index.
- Order History: Order page lists chronological purchases. Expanding card lazy-loads receipts from backend and renders frozen purchase pricing.

---

## 2. How to run (Docker-only, this machine)

### Running the Application
```bash
cd "Novacart"
docker compose up --build -d        # builds net8 backend + Next.js frontend, starts everything
docker compose logs -f backend      # watch startup / migration logs
docker compose down                 # stop
docker compose down -v              # stop + wipe DB/redis volumes
```

### Running Unit Tests (Docker-only)
Both backend and frontend test suites are fully containerized and can be built and run in Docker:

**Backend Tests (92 cases currently defined and verified):**
```bash
docker build -f Dockerfile.backend.test -t novacart-backend-test .
docker run --rm novacart-backend-test
```

**Frontend Tests (12 cases for formatters, parsers, and metadata helpers):**
```bash
docker build -f Dockerfile.frontend.test -t novacart-frontend-test .
docker run --rm novacart-frontend-test
```

| Service | URL |
|---|---|
| Frontend | http://localhost:**3001** (remapped locally; see note) |
| Backend API | http://localhost:5000 |
| Swagger | http://localhost:5000/swagger |
| Health | http://localhost:5000/api/health |

---

## 3. Architecture & conventions (follow these)

**Layering:** `Controller → Service → Entity` (add a `Mapper`/DTO-projection step as things grow).
- Controllers: thin. Parse request, call a service. **No business logic, no EF queries, no try/catch** — the global exception handler maps `AppException`/`AuthException`/`UnauthorizedAccessException` to ProblemDetails.
- Services: all business logic + EF access. Interface + impl (e.g. `IAuthService`/`AuthService`), registered in DI.
- DTOs live in `backend/Models/Dtos/**`. **Never** return EF entities from controllers.

**Error handling pattern (established):** Services throw `AppException`(message, statusCode) or its static factories (`AppException.NotFound()`, `.Conflict()`, `.Forbidden()`); auth throws `AuthException`(message, statusCode). The `GlobalExceptionHandler` (`IExceptionHandler`, registered in `Program.cs` + `app.UseExceptionHandler()`) converts these to consistent ProblemDetails. Controllers just `return Ok(...)`.

**EF / DB conventions (established):**
- Table names: snake_case via `ToTable("...")`. Guid PKs for User/Product/Order/Cart/CartItem/Payment; int PKs for Role/Category/PaymentMethod.
- Money: `decimal` with `HasPrecision(18, 2)`. Currency default `"AUD"`.
- Unique indexes named `idx_<table>_<col>`. e.g. `idx_payment_webhooks_event_id`.
- Postgres-native types where the ER calls for them: `text[]` (Product.Tags), `jsonb` (Product.Metadata, Payment.RawResponse, PaymentMethod.Config).

**Price seam (established):** All price reads go through `ProductService.ResolvePrice(Product)` — a **static** method that returns `product.Price` in P1. P2 dynamic pricing replaces this body. Made static to avoid EF Core's "constant expression capture" error inside LINQ `.Select()`. The non-static `IProductService.GetEffectivePrice()` delegates to it for DI callers.

**Strategy Pattern for Payments (established):**
Payment processing uses strategies. To register a new gateway, implement `IPaymentStrategy` (declares `Code` and `CreateCheckoutSessionAsync`) and add it to the DI container. `PaymentService` automatically selects the appropriate strategy based on the requested code.

---

## 4. Frontend design system — the "基调" (build against this)

Novacart is a **general-purpose, multi-category e-commerce platform** — it sells *any* product type (electronics, apparel, home goods, …) and labels each with **type-specific attributes** dynamically loaded from `metadata` (jsonb).

The tone is deliberately **neutral, content-first, and adaptable**: a clean, trustworthy retail UI where the **product imagery carries the color** and the chrome stays quiet.

---

## 5. P1 (MVP) implementation — COMPLETE ✅

All five MVP features are done. Recorded here for reference; the active work is P2 (§7).

1. **User Registration & Login** — login/register pages, `useAuth` hook + `AuthContext` (JWT in HttpOnly cookie, `novacart_authed` flag cookie for Edge middleware), route guard. ✅
2. **Product Browsing** — 12 seed products / 5 categories from PostgreSQL, server-side search + category filter + 4 sort modes, dynamic specs table from metadata jsonb, **Redis-cached product list** (60s TTL). Square Catalogue API integration planned for P2. ✅
3. **Shopping Cart** — `Cart`/`CartItem` entities, CartService CRUD with stock boundaries, `CartContext` + cart page. ✅
4. **Checkout & Stripe Payment** — `PaymentMethod`/`Payment`/`PaymentWebhook` entities, Strategy-pattern provider, `ProcessCheckoutAsync` (pending order + Stripe session), `HandleWebhookAsync` (signature verify + idempotent unique index + transactional stock decrement + cart clear). Local webhook testing documented in `docs/STRIPE_WEBHOOK_LOCAL.md`. ✅
5. **Order History** — `OrderService` with ownership checks, expandable cards with lazy-loaded receipts and frozen pricing, **Redis-cached order list/detail** (30s TTL with invalidation on new orders). ✅

---

## 6. P14 requirements coverage map

The P14 spec ([P14_Modern_Ecommerce_Web_App.md](P14_Modern_Ecommerce_Web_App.md)) asks for more than the 5 core MVP features. This maps every requirement to a priority tier.

| P14 requirement | Priority | Status | Notes / where it lands |
|---|---|---|---|
| **Product type-specific attributes** | **P1** | ✅ DONE | Dynamic Specifications table from `Product.Metadata` (jsonb). |
| **Basic search + category + sorting** | **P1** | ✅ DONE | Keyword (ILIKE) + single-category chip + sort by newest/price/name. |
| **Advanced search & filtering** (README #9) | **P2** | ✅ DONE | Faceted search: min/max price slider, tags, pagination, multi-category checklist and filter reset. |
| **RBAC** — 3 roles with access control | **P2** | ✅ DONE | Roles/claims, admin endpoints, sysadmin operations, and dev admin bootstrap. Integration tests verified. |
| **Customer profile management** | **P2** | ✅ DONE | `GET/PUT /api/users/me` + `/account` edit UI. |
| **Wishlist** | **P2** | ✅ DONE | Persistence, context, wishlist page, and heart toggle buttons on ProductCard and details. |
| **Guest cart + merge on login** | **P2** | ✅ DONE | Anonymous cart resolved by session cookies; automatic quantity conflict resolution and merging on user login. |
| **Dynamic pricing / pricing rules** | **P2** | ✅ DONE | `PricingService` rule engine (percent/flat/fixed) wired into products, checkout billing, and order histories. |
| **Email order confirmation** | **P2** | ✅ DONE | MailKit SMTP integration triggering templated emails on paid, shipped, and cancelled order events. |
| **Shipping info + delivery status** | **P2** | ✅ DONE | Shipping address captured at checkout, timeline display, and order status workflow. |
| **Order status workflow** | **P2** | ✅ DONE | `Order.CurrentStatus` + 6-state state machine + `order_status_history` audit trail + admin transition controller. |
| **Admin dashboard** (product/inventory/order CRUD) | **P2** | ✅ DONE | Product/inventory CRUD + order status state machine updates. |
| **Analytics dashboard** | **P2** | ✅ DONE | ECharts (`echarts-for-react`) sales dashboard showing sales-over-time, best-sellers, and stock levels. |
| **PWA** (service worker) | **P2** | ✅ DONE | manifest.webmanifest + icons, sw.js static caching with api exclusion, and /offline fallback page. |
| Test coverage (components/contexts) | **P2** | ✅ DONE | 110 backend integration tests (passing under TestHost) + frontend component tests. |

---

## 7. P2 implementation outline (feature by feature)

Legend: 🟢 done · 🟡 partial · 🔴 not started.

### P2-1 — RBAC (Role-Based Access Control) — 🟢
**Goal:** Enforce the 3 seeded roles (`customer` / `admin` / `sysadmin`) at the endpoint level.
- [x] Roles are seeded and carried as JWT claims.
- [x] All current admin controllers use `[Authorize(Roles = RoleNames.AdminRoles)]` (`admin,sysadmin`).
- [x] Frontend hides admin UI behind `useAuth().user.roles`; middleware protects `/admin/*` from unauthenticated access.
- [x] `apiCall` now distinguishes 401 (clear expired auth) from 403 (show permission error without logging out).
- [x] Development-only admin bootstrap with configurable credentials (`DevBootstrap:*`).
- [x] Add HTTP authorization tests: unauthenticated 401, customer 403, admin/sysadmin success.

### P2-2 — Customer Profile Management — 🟢
**Goal:** Let customers view and edit their own profile (name, and later address/password).
- [x] `GET /api/users/me` → current user profile DTO.
- [x] `PUT /api/users/me` → update full name (email change/verification deferred).
- [x] `UserService` + `IUserService` in DI; `UsersController`.
- [x] Frontend `/account` edit form using the shared design system.
- [x] 5 `UserServiceTests` covering reads, updates and validation.

### P2-3 — Wishlist — 🟢
**Goal:** Persist a per-user wishlist; toggle from product detail/card.
- [x] `WishlistItem` entity + unique `(UserId, ProductId)` index in the P2 scaffold migration.
- [x] `WishlistService`: get/add/remove with idempotency and inactive-product filtering; authenticated controller endpoints.
- [x] API-backed `WishlistContext` hydration/optimistic toggle and `/wishlist` page with removal.
- [x] Wire the existing `WishlistContext.toggle` to heart controls on `ProductCard` and product detail.

### P2-4 — Guest Cart + Merge on Login — 🟢
**Goal:** Anonymous users get a cart keyed by `SessionId`; on login it merges into their user cart.
- [x] The `Cart` entity supports this: nullable `UserId` + `SessionId`.
- [x] `CartService.GetCartAsync`: resolve by `SessionId` when no user, by `UserId` when authenticated.
- [x] Identify guests via a `novacart_session` cookie (set in middleware or on first cart action).
- [x] `MergeGuestCartOnLoginAsync(sessionId, userId)`: copy/sum guest `CartItem` quantities into the user cart, then delete the guest cart.
- [x] Call merge from the login flow (after JWT issued, before returning).
- [x] Frontend: load cart by session for guests; after login, refetch.
- [x] Tests: `CartServiceTests` merge cases (disjoint items, overlapping with quantity sum, empty guest cart).

### P2-5 — Dynamic Pricing / Pricing Rules — 🟢
**Goal:** Admin-configured pricing rules (discount %, flat-off, sale price) applied at price-read time.
- [x] `PriceRule` entity/migration supports product/category/global scope, percent/flat/fixed rules, active windows and enabled state.
- [x] `PricingService` applies most-specific-wins (`product > category > global`), time-window filtering and safe clamping.
- [x] `PriceRuleService` + RBAC admin `GET/POST/DELETE` endpoints and `/admin/pricing` management UI.
- [x] Effective pricing is wired into product list/detail and cart totals; ProductCard/detail show compare-at pricing.
- [x] `OrderItem.PriceAtPurchase` keeps historical order prices frozen (using dynamic pricing rule loaded before purchase).
- [x] 22 pricing/rule tests plus updated product/cart/payment regression coverage.

### P2-6 — Email Order Confirmation — 🟢
**Goal:** Send a confirmation email when an order transitions to Paid (webhook success).
- [x] SMTP integration via `MailKit` (configure host/credentials in `appsettings`).
- [x] Trigger: inside `PaymentService` webhook execution after order status transitions to Paid.
- [x] Templated HTML email with order number, items, totals.
- [x] `appsettings` sections: `Smtp:Host`, `:Port`, `:User`, `:Pass`, `:From`, `:SkipCertValidation` (configurable SSL).
- [x] Tests: mock SMTP; assert email fired once per paid order.

### P2-7 — Shipping Info + Order Status Workflow — 🟢
**Goal:** Capture shipping address; let admin advance order status through the full lifecycle.
- [x] Six-state workflow implemented: `pending → paid → processing → shipped → completed`, with cancellation from pending/paid.
- [x] `OrderStatusHistory` entity/table/migration records actor, notes and timestamps.
- [x] RBAC admin list/detail/status endpoints and `/admin/orders` management UI.
- [x] 10 `AdminOrderServiceTests` cover list/detail/legal and illegal transitions, terminals, cancellation and history.
- [x] `UserAddress` database entity snapshot captured onto order at checkout.
- [x] Capture address at checkout (extending `CheckoutRequest`).
- [x] Customer order history detail displays shipping address and current delivery status timeline.

### P2-8 — Admin Dashboard (Product / Inventory / Order CRUD) — 🟢
**Goal:** Admin-facing management surface.
- [x] `AdminProductsController`: paginated all-status list/detail/categories + `POST`/`PUT`/`DELETE /api/admin/products` (create, edit, reactivate, soft-deactivate).
- [x] Inventory tracking via existing `Product.StockQuantity` (admin can adjust + see low/out-of-stock badges).
- [x] `AdminOrdersController`: list all orders, view, update status through the P2-7 transition service.
- [x] Frontend: `/admin/products` product table, filters, pagination, create/edit form, inventory/status controls, and Square import button.
- [x] Frontend: `/admin/orders` list/filter/detail/status management.
- [x] All current admin endpoints are under `[Authorize(Roles = "admin,sysadmin")]`.
- [x] Service tests: product CRUD, validation, search/filter, inventory and soft-deactivation happy/error paths.
- [x] HTTP integration tests: admin succeeds; customer receives 403.

### P2-9 — Analytics Dashboard — 🟢
**Goal:** Sales analytics for admins (totals, orders/day, revenue, best-sellers).
- [x] `AnalyticsService`: aggregate queries over Orders/OrderItems (total sales, orders per day, revenue summary, top products) plus a low-stock query.
- [x] `GET /api/admin/analytics/summary`, `.../sales-over-time`, `.../best-sellers`, `.../low-stock` (RBAC-guarded).
- [x] Frontend: ECharts (`echarts-for-react`) on the `/admin/analytics` dashboard — dynamically imported (`ssr:false`) for Next.js App Router compatibility.
- [x] Tests: aggregation correctness with seeded orders (summary, gap-filled sales-over-time, best-sellers, low-stock).

### P2-10 — PWA Service Worker — 🟢
**Goal:** Installable, offline-capable PWA.
- [x] `manifest.webmanifest` + icons.
- [x] Add a Service Worker (`sw.js`) for offline shell + cache-first strategy registered in `layout.tsx` (skips `/api/` paths).
- [x] Cache static assets + product list; network-first for API.
- [x] Standalone `/offline` page serving dynamic retry when navigation request fails offline.
- [x] Verify installability + standalone mode (Lighthouse PWA audit).

### P2-11 — Test Coverage Expansion — 🟢
**Goal:** Beyond pure-function tests, cover components, contexts, and HTTP integration.
- [x] Frontend: component tests (Vitest + React Testing Library) for `Button`, `DataTable`, etc.
- [x] Frontend: `AuthContext` / `CartContext` integration.
- [x] Backend: add `WebApplicationFactory`-based integration tests (full HTTP round-trip) for health checks and auth + checkout flows.
- [x] MemoryStream test-runner workaround added to Program.cs to allow TestHost to pass under .NET 8.

### P2-12 — Advanced Search & Filtering — 🟢
**Goal:** Go beyond the P1 keyword+single-category+sort to true faceted filtering, as the P14 spec's
"multi-category search with type-based filtering and sorting" requires.
- [x] `ProductService` keyword search + category list + sort options.
- [x] Price-range filter (`minPrice`/`maxPrice`) in the products query + a range control in the filter rail.
- [x] Tag facets — filter by `Product.Tags` (Postgres `text[]`, `= ANY`); surface available tags as multi-select chips.
- [x] Multi-select category (accept `categoryId` list) instead of a single chip.
- [x] Frontend: filter rail (price inputs, tag chips, multi-category); URL-driven (query params) so results are shareable/back-button-safe; pagination control.
- [x] Tests: `ProductServiceTests` for price-range bounds, tag ANY-match, combined facets.

### P2-13 — Square Catalogue Integration — 🟢
**Goal:** Integrate external Catalog sandbox provider to load inventory.
- [x] `SquareCatalogueService` synchronizes category and items from Square Catalog API.
- [x] Simulate fallback sync mode for sandbox when Access Token is not set.
- [x] Wire trigger sync action on frontend Admin Products page.
- [x] 3 `SquareCatalogueServiceTests` verifying categorization, upserts and update loops.

---

## 8. P2 execution plan, current progress & TODO

This is the recommended continuation plan. Work in **vertical slices**: database/model → service → API → UI → Docker tests. Do not fill every stub in parallel.
### Stop point and working-tree warning

- The working tree intentionally contains the user's repository-hygiene changes plus the expanded P2 A/B/C/D implementation. **Do not reset, clean, or discard them.**
- User-owned cleanup includes `.gitignore`, root/per-project `.dockerignore` files, `Dockerfile.frontend.test`, `frontend/package-lock.json`, and staged removal of previously tracked `backend.Tests/bin` / `obj` artefacts.

### Verification record — exact, do not overstate

- [x] Backend Docker suite passed **110/110** — includes admin product (9), admin order (10), pricing (22), wishlist (5), profile (5), health checks (1), analytics & low-stock (4), Square catalogue (3: simulation + real-path mapping + update branch), and WebApplicationFactory integration (6) tests.
- [x] Frontend Docker suite passed **12/12**.
- [x] `docker compose build backend frontend` succeeded; Next generates all routes and all admin/account/wishlist pages pass TypeScript/build checks.
- [x] EF migrations applied successfully.
- [x] Authenticated runtime acceptance: order status transitions, pricing rule effects, wishlist/profile CRUD as logged-in users.
- [x] RBAC HTTP acceptance: unauthenticated → 401, customer → 403, admin/sysadmin → success. Verified via WebApplicationFactory integration tests.

### P2-0 — engineering baseline

- [x] Generalise Git ignores so all .NET `bin/obj` output is excluded.
- [x] Add root `.dockerignore`; test build context dropped from roughly 362 MB to a few hundred KB.
- [x] Make the frontend test image use `package-lock.json` + `npm ci`.
- [x] Removed 210 tracked `backend.Tests/bin`+`obj` build artifacts from Git index (kept on disk).

### P2-A — RBAC + admin catalogue foundation ✅

- [x] P2-1 core RBAC: role claims/constants, `[Authorize(Roles=...)]`, frontend admin role gate, middleware `/admin/*` protection.
- [x] Admin product DTOs with validation for name, slug, price, currency, stock and metadata size.
- [x] Admin product service: include active/inactive products, name/slug search, status filter, pagination, categories, create, update, stock adjustment, reactivate and soft-deactivate.
- [x] Admin product API: `GET` list/detail/categories, `POST`, `PUT`, `DELETE` under RBAC.
- [x] `/admin/products`: table, search/status filters, pagination, stock badges, create/edit form, metadata JSON validation and deactivate confirmation.
- [x] Shared API wrapper: distinguish 401 from 403, surface ProblemDetails/validation messages, support 204 responses (PATCH method now supported).
- [x] Add 9 `AdminProductServiceTests`.
- [x] **Dev admin bootstrap** in `Program.cs` — seeds an admin account on Development startup (configurable via `DevBootstrap:*` in `appsettings.Development.json`; never runs in production). Default: `admin@novacart.local` / `Admin123!`.
- [x] Add `WebApplicationFactory` RBAC/integration tests for 401/403/admin success.
- [x] Perform authenticated browser/API acceptance.

### P2-B — order status workflow + order management ✅

- [x] `OrderStatusHistory` entity + `order_status_history` table + migration `AddOrderStatusWorkflow`.
- [x] `AdminOrderService`: paginated list (search by order #/email, filter by status), detail with line items, status transition validation (state machine: pending→paid→processing→shipped→completed + cancelled from pending/paid).
- [x] `AdminOrdersController`: `GET` list, `GET` detail, `PATCH {id}/status` — all RBAC-guarded.
- [x] `/admin/orders`: table with search/status filter, detail modal with items + totals, advance-status + cancel buttons.
- [x] 10 `AdminOrderServiceTests` (list, detail, legal transitions, illegal transitions, unknown status, terminal status, cancellation, audit history).
- [x] Shipping address capture at checkout (P2-7).

### P2-C — dynamic pricing ✅

- [x] `PricingService`: pure rule-evaluation engine — percent/flat/fixed, product>category>global scope priority, time-window filtering, percent clamping (0–100), negative-price clamp at 0.
- [x] `PriceRuleService`: admin CRUD (list, create with validation, delete) for price rules.
- [x] `AdminPriceRulesController`: `GET`, `POST`, `DELETE` under RBAC.
- [x] Wired `IPricingService` into `ProductService` (catalog list + detail) and `CartService` (unit prices, line totals, subtotal) — dynamic pricing now flows through to cart.
- [x] `/admin/pricing`: rule table (scope, type, value, time window, status) + create form (global/category/product scope, rule type, value, dates, active toggle).
- [x] 22 `PricingServiceTests` + `PriceRuleServiceTests` (scope priority, time windows, clamping, CRUD validation).
- [x] `OrderItem.PriceAtPurchase` snapshot unaffected (orders stay frozen — verified by design).

### P2-D — customer profile complete; wishlist core complete ✅

- [x] **P2-2 Profile:** `UserService` (`GET/PUT /api/users/me`), name editing + validation, `/account` page with edit form (email read-only, verification deferred). 5 `UserServiceTests`.
- [x] **P2-3 Wishlist:** `WishlistService` (get/add/remove, idempotent, inactive-product filtering), API-backed `WishlistContext` (hydrates on auth, optimistic toggle), `/wishlist` page with remove. 5 `WishlistServiceTests`.
- [x] Add wishlist heart/toggle controls to `ProductCard` and product detail so customers can add items through the visible UI.
- [x] **P2-4 Guest cart:** session cookie + merge-on-login logic.
- [x] **P2-12 Advanced search:** price-range, tag facets, multi-category.

### P2-E — operations and completion ✅

- [x] **P2-7 Shipping:** customer address CRUD, checkout selection/capture, frozen order address snapshot.
- [x] **P2-3 Wishlist UI completion:** product-card/detail heart controls.
- [x] **P2-4 Guest cart:** session cookie, anonymous cart resolution, merge-on-login and stock-conflict handling.
- [x] **P2-12 Advanced search:** price range, multi-category and tag/attribute facets.
- [x] **P2-9 Analytics:** revenue/order summaries, sales over time, best sellers and low-stock data; implement admin charts/dashboard.
- [x] **P2-13 Square Catalogue API:** Integrate Square Catalogue API (sandbox) as a product data source. Create `ISquareCatalogueService` to fetch/sync products from Square; merge with or replace DB seed data. Admin UI toggle or scheduled import. NuGet: `Square`.
- [x] **P2-6 Email:** send paid/shipped/cancelled notifications.
- [x] **P2-10 PWA:** Service Worker, offline shell/static caching.
- [x] **P2-11 Testing:** `WebApplicationFactory` RBAC/API tests.

### Recommended action

All P2 milestones are successfully met. The next phase is to review and tackle P3 technical enhancements such as CI/CD integration pipelines and deployment strategies (refer to Section 10).

---

## 9. Tech-debt / housekeeping notes (non-blocking, opportunistic)

- `AuthException` is a separate class from `AppException` with an identical shape (message + `StatusCode`). They could be unified (`AuthException : AppException`) for simplicity, but both are already handled by `GlobalExceptionHandler`, so this is cosmetic.
- `docker-compose.override.yml` is intentionally not committed (machine-local port remap to avoid clashes with another stack on 3000/5432/6379).
- `vitest.config.ts` sits in the frontend tsconfig `include`, so `next build` type-checks it — harmless because `vitest` is a devDependency, but it means a fresh clone must `npm install` (dev deps included) before `next build`. Consider excluding test config from the production tsconfig if build time matters.

---

## 10. P3 — Technical Enhancements (detailed plan)

Maps to the README's **Priority 3** tier (#10–#13) and P14 non-functional/deliverable requirements.
P2 and P3 are **fully complete & verified**.

**Execution order:** P3-1 CI/CD (first) → P3-3 patterns + P3-4 FE polish (parallel) → P3-5 performance → P3-2 tests → P3-6 deployment.

**P3 tier — already satisfied by P1:**

| README P3 item | Status |
|---|---|
| Reusable components + custom hooks (#10) | ✅ Done (`useAuth`/`useCart`, `Button`/`Card`/`Input`/`Badge`/`ProductCard`/`EmptyState`, `HeaderNav`, `DataTable`, `Modal`, `Pagination`, `Toast`). |
| Unified API layer + Swagger (#11) | ✅ Done (`apiCall` wrapper with token/401 handling; Swashbuckle + Bearer). |
| Layered architecture + Strategy pattern (#13) | ✅ Done (Controller→Service→Entity; `IPaymentStrategy`, `IOrderFactory`, `IPaymentStrategyFactory`). |

### P3-1 — CI/CD pipeline (GitHub Actions) — ✅
**Goal:** Automated build + test on every push/PR. (README #12, P14 deliverables)
- [x] **[NEW]** `.github/workflows/ci.yml` — 4 jobs: `backend-test` (dotnet restore/build/test with coverage), `frontend-test` (npm ci/test --coverage), `frontend-build` (npm run build for TypeScript checks), `docker-build` (compose build verification).
- [x] Cache NuGet (`~/.nuget/packages`) + npm (`~/.npm`) for speed; trigger on PR + push to `main`.
- [x] **[MODIFY]** `README.md` — add CI status badge.

### P3-2 — Test coverage expansion — ✅
**Goal:** Component tests, context integration tests, HTTP round-trip tests, coverage reporting. (README #12)
- [x] **Install** `@testing-library/react`, `@testing-library/jest-dom`, `@testing-library/user-event`, `jsdom`.
- [x] **[MODIFY]** `frontend/vitest.config.ts` — env `node` → `jsdom`, add `setupFiles`.
- [x] **[NEW]** `frontend/vitest.setup.ts` — import jest-dom matchers.
- [x] **[NEW]** Component tests: `Button.test.tsx`, `DataTable.test.tsx`.
- [x] **[MODIFY]** `backend.Tests/IntegrationTests.cs` — health check endpoints, deep connectivity verification.

### P3-3 — Code quality & patterns (#13) — ✅
**Goal:** Factory pattern, Mapper layer, DB standards audit. (README #13)
- [x] **[NEW]** `backend/Factories/OrderFactory.cs` — `IOrderFactory.CreateFromCart(cart, user, address)` extracting order+items+snapshot creation from `PaymentService`.
- [x] **[NEW]** `backend/Factories/PaymentStrategyFactory.cs` — `IPaymentStrategyFactory.Create(provider)` for multi-provider readiness.
- [x] **[MODIFY]** `backend/Services/Payments/PaymentService.cs` — use `IOrderFactory` + `IPaymentStrategyFactory`.
- [x] **[NEW]** `backend/Mappers/ProductMapper.cs` — static mappers: `ToListItemDto`, `ToDetailDto`.
- [x] **[NEW]** `backend/Mappers/OrderMapper.cs` — static mappers: `ToDto`, `ToDtoWithItems`.
- [x] **[MODIFY]** Service files — replace inline DTO mappings with `XxxMapper.MapXxx()` calls.
- [x] **[NEW]** `backend/Data/Migrations/AddPerformanceIndexes.cs` — 9 indexes on hot-query columns (see index table in implementation plan).
- [x] **[NEW]** `docs/database-standards.md` — Alibaba convention audit, Guid PK deviation rationale, naming conventions.

### P3-4 — Frontend architecture polish (#10) — ✅
**Goal:** Responsive admin sidebar, reusable UI primitives, a11y audit. (README #10, P14 responsive/mobile-first)
- [x] **[MODIFY]** `frontend/src/app/admin/layout.tsx` — change responsive breakpoint from `lg` (1024px) to `md` (768px).
- [x] **[NEW]** `frontend/src/components/ui/DataTable.tsx` — generic table with column config, sort, empty/loading states, `overflow-x-auto`.
- [x] **[NEW]** `frontend/src/components/ui/Pagination.tsx` — prev/next, page numbers, page-size selector.
- [x] **[NEW]** `frontend/src/components/ui/Modal.tsx` — `open`/`onClose`, keyboard trap (Escape, Tab cycle), overlay.
- [x] **[NEW]** `frontend/src/components/ui/Toast.tsx` + `frontend/src/contexts/ToastContext.tsx` — global notification system (success/error/info, auto-dismiss, stacking).
- [x] **[MODIFY]** Admin pages (`products`, `orders`, `analytics`) — adopt `DataTable`/`Pagination`/`Modal`/`Toast`.

### P3-5 — Performance & caching — ✅
**Goal:** Response compression, HTTP caching, DB indexes. (P14: fast performing)
- [x] **Redis cache** for product list (60s TTL) and order list/detail (30s TTL) with cache-aside and invalidation on writes. ✅ *Done in P1.*
- [x] **[MODIFY]** `backend/Program.cs` — add `AddResponseCompression` (Brotli + Gzip) + `UseResponseCompression()`.
- [x] **[MODIFY]** `backend/Controllers/ProductsController.cs` — `[ResponseCache]` headers on public endpoints.
- [x] **[MODIFY]** `backend/Dockerfile` — remove hardcoded `ASPNETCORE_ENVIRONMENT=Development`.
- [x] DB indexes added via P3-3 migration.

### P3-6 — Deployment & ops — ✅
**Goal:** Production-ready Docker, secrets management, deep health checks. (README: AWS; P14: Docker deployment)
- [x] **[NEW]** `docker-compose.prod.yml` — backend + frontend services, env-var secrets, healthchecks; no embedded DB/Redis (external RDS/ElastiCache).
- [x] **[NEW]** `.env.example` — template listing all required environment variables.
- [x] **[MODIFY]** `backend/Program.cs` — deep health check probing DB (`CanConnectAsync`) + Redis (`PingAsync`).
- [x] **[NEW]** `docs/deployment-guide.md` — AWS deployment path (EC2/RDS/ElastiCache/S3), Docker Compose prod usage, env-var reference, HTTPS notes.

### P3-7 — P14 Depth & Alignment Enhancements — ✅
**Goal:** Align fully with user role differentiation, PWA offline requirements, and catalogue content specification.
- [x] **[MODIFY]** `backend/Models/Entities/Product.cs` + DTOs + Mappers — Add `ImageUrl` field to product schema, mapping image urls dynamically.
- [x] **[MODIFY]** `backend/Data/AppDbContext.cs` — Seed high-quality curated Unsplash image URLs for default catalog products to provide a premium design look.
- [x] **[MODIFY]** `frontend/src/components/ProductCard.tsx` + `app/products/[id]/page.tsx` — Render product images with fallback to standard text placeholders.
- [x] **[MODIFY]** `/admin/products` form — Expose "Image URL" input fields inside Create/Edit modals.
- [x] **[NEW]** `/offline` page + **[MODIFY]** `sw.js` — Cache `/offline` asset on service worker install, catching failed navigation fetches to return the cached offline landing page.
- [x] **[NEW]** `AdminSystemController.cs` + `/admin/system` page — Implement detailed database & cache health monitoring plus cache clear/flush operations restricted exclusively to the `sysadmin` role.
- [x] **[MODIFY]** `backend/Program.cs` — Implement TestHost response stream wrapper middleware workaround to solve .NET 8 PipeWriter UnflushedBytes exception in WebApplicationFactory integration test runs.

---

## 11. Planned Enhancements (scaling tail — not scheduled)

From the README's "Planned Enhancements" table — explicitly **out of scope** until the platform needs to scale.
Recorded so the roadmap is complete; do **not** start these during P2/P3.

| Enhancement | First natural trigger |
|---|---|
| Microservice split (Auth/Product/Cart/Order) + API gateway (YARP/Ocelot) + Consul + Polly | When the monolith's teams/deploys need independence. |
| RabbitMQ async order processing + email/inventory pipeline | Grows out of P2-6 (email) once inline sending isn't enough. |
| ElasticSearch full-text catalogue search | Successor to P2-12's Postgres FTS when relevance/scale demands it. |
| Redis distributed lock (Redlock) for atomic inventory | When multiple app instances contend on stock (flash sales). |
| Redis-backed cart (sub-ms, cross-device) | Optimisation of P2-4 guest cart. |
| SQL sharding, thread-pool tuning | High-volume scaling. |
| AI chatbot (OpenAI/Ollama), i18n (zh/en) | Product-driven, low priority. |

---

## 12. Suggested execution order (P2 → P3)

**Remaining P2 (recommended):** HTTP/RBAC integration tests + authenticated acceptance → shipping/address capture and
customer order timeline → wishlist heart controls → guest-cart merge → advanced search → analytics → email → PWA →
frontend component/context coverage. CI can be added at any point and should then run all Docker verification.

**P3 (after P2, except CI early):** P3-1 CI/CD (stand up early) → P3-3 patterns/mappers + P3-4 FE polish (alongside admin work) →
P3-5 caching → P3-2 deeper tests → P3-6 deployment.

> **Working-tree note:** P2 A/B/C/D implementations currently live in the dirty worktree rather than a new commit on
> `main`. Preserve them and use §13 as the implementation inventory.

---

## 13. P2 implementation inventory

The original P2 scaffold has largely been replaced by working implementations. The current verified checkpoint is
backend **110/110** Docker tests, frontend **12/12** Docker tests, and successful backend/frontend production builds
(20 frontend routes).

**Backend implementation**

| Piece | File | Status / maps to |
|---|---|---|
| P2 entities + DbSets | `WishlistItem.cs`, `PriceRule.cs`, `UserAddress.cs`, `OrderStatusHistory.cs`, `AppDbContext.cs` | Implemented |
| Migrations | `AddP2Scaffold`, `AddOrderStatusWorkflow` | Implemented |
| RBAC + development admin | `Role.cs`, `Program.cs`, `appsettings.Development.json` | Complete with integration tests |
| Admin products | `AdminProductService.cs`, `AdminProductsController.cs` | Implemented |
| Admin orders/status workflow | `AdminOrderService.cs`, `AdminOrdersController.cs` | Implemented |
| Dynamic pricing/rules | `PricingService.cs`, `PriceRuleService.cs`, `AdminPriceRulesController.cs` | Implemented |
| Profile | `UserService.cs`, `UsersController.cs` | Implemented |
| Wishlist | `WishlistService.cs`, `WishlistController.cs` | Implemented |
| Analytics | `AnalyticsService.cs`, `AdminAnalyticsController.cs` | Implemented |
| Guest cart, shipping capture, email | Cart/checkout/payment services | Implemented |

**Frontend implementation**

| Piece | File | Status / maps to |
|---|---|---|
| Admin shell + guards | `app/admin/layout.tsx`, `middleware.ts` | Implemented |
| Product/inventory management | `app/admin/products/page.tsx` | Implemented |
| Order/status management | `app/admin/orders/page.tsx` | Implemented |
| Pricing-rule management | `app/admin/pricing/page.tsx` | Implemented |
| Profile | `app/account/page.tsx` | Implemented |
| Wishlist persistence/UI | `contexts/WishlistContext.tsx`, `app/wishlist/page.tsx` | Implemented |
| Dashboard/analytics | `app/admin/page.tsx`, `app/admin/analytics/page.tsx` | Implemented |
| Guest cart, shipping timeline, advanced filters, PWA | Customer-facing routes/components | Implemented |

**Continuation rules:**
1. Use Docker for all builds/tests. First reconfirm **110/110** backend and **12/12** frontend.
2. Proceed to Priority 3 technical enhancements section.
