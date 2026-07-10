# Novacart — Handoff & Roadmap

> **Purpose of this file.** A self-contained handoff so another AI/developer can continue building or
> auditing Novacart without re-discovering context. It records what is already done, how to run the
> project, the conventions to follow, the frontend design system to build against, and the
> **Priority 2 (P2)** feature-by-feature implementation plan with a flat checklist.
>
> **Status:** Priority 1 (MVP) is **complete & verified**. Priority 2 (P2) features are largely implemented. See §8 for the current P2 status.
>
> Last updated: 2026-07-10 — P2-A admin/RBAC foundation, P2-B order workflow, P2-C dynamic pricing, P2-D profile and Wishlist core are implemented with tests. Backend **92/92** tests passing, frontend **12/12** passing. Wishlist heart controls, HTTP-level RBAC tests and the remaining P2-E slices are still open.

---

## 0. TL;DR for whoever picks this up

- **Stack:** Backend ASP.NET Core 8 + EF Core + PostgreSQL + Redis. Frontend Next.js 14 (App Router) + TS + Tailwind.
- **Priority 1 (MVP) is 100% completed and verified** — the 5 core features (auth, products, cart, checkout, orders) all work end-to-end.
- **Priority 2 (P2) is largely implemented:**
  - P2-A: RBAC + admin product CRUD + **dev admin bootstrap** (done)
  - P2-B: **Order management + status workflow** with audit history (done — backend + UI + tests)
  - P2-C: **Dynamic pricing** (rule engine wired into products + cart; admin CRUD UI) (done)
  - P2-D: **Customer profile** done; Wishlist persistence/page/tests done, but product-card/detail heart controls remain
  - Remaining P2: shipping/address capture, wishlist heart controls, guest cart, email, analytics, PWA, advanced search, and expanded HTTP/frontend tests
- **The schema is future-proofed** for remaining P2 features (6-state order workflow with audit history, guest-cart columns, price rules, wishlist, user addresses) — see §7.
- **Everything runs in Docker** — one command: `docker compose up --build -d`.

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
| JWT bearer auth + authorization middleware wired | ✅ | [backend/Program.cs](backend/Program.cs) |
| Auth frontend: login + register pages, useAuth hook, middleware route guard, header user menu | ✅ | [frontend/src/app/login/](frontend/src/app/login/), [frontend/src/app/register/](frontend/src/app/register/), [frontend/src/contexts/AuthContext.tsx](frontend/src/contexts/AuthContext.tsx), [frontend/src/middleware.ts](frontend/src/middleware.ts) |
| ProductService + DTOs (paginated list with ILike search, category filter, 4 sort modes, detail with metadata) | ✅ | [backend/Services/ProductService.cs](backend/Services/ProductService.cs), [backend/Models/Dtos/Products/ProductDtos.cs](backend/Models/Dtos/Products/ProductDtos.cs) |
| ProductsController (DB-backed, search/filter/sort/pagination) | ✅ | [backend/Controllers/ProductsController.cs](backend/Controllers/ProductsController.cs) |
| **P2 Admin product/inventory management** (all-status list/search, create, edit, stock adjustment, reactivate, soft-deactivate) | ✅ CODE COMPLETE | [backend/Services/AdminProductService.cs](backend/Services/AdminProductService.cs), [backend/Controllers/Admin/AdminProductsController.cs](backend/Controllers/Admin/AdminProductsController.cs), [frontend/src/app/admin/products/page.tsx](frontend/src/app/admin/products/page.tsx) |
| **P2 Admin order management + status workflow** (list/detail/filter, validated transitions, audit history) | ✅ CODE COMPLETE | [backend/Services/AdminOrderService.cs](backend/Services/AdminOrderService.cs), [backend/Controllers/Admin/AdminOrdersController.cs](backend/Controllers/Admin/AdminOrdersController.cs), [frontend/src/app/admin/orders/page.tsx](frontend/src/app/admin/orders/page.tsx) |
| **P2 Dynamic pricing** (percent/flat/fixed rules, scope priority, active windows, product/cart integration, admin UI) | ✅ CODE COMPLETE | [backend/Services/PricingService.cs](backend/Services/PricingService.cs), [backend/Services/PriceRuleService.cs](backend/Services/PriceRuleService.cs), [frontend/src/app/admin/pricing/page.tsx](frontend/src/app/admin/pricing/page.tsx) |
| **P2 Customer profile** (authenticated read/update API and account edit UI) | ✅ CODE COMPLETE | [backend/Services/UserService.cs](backend/Services/UserService.cs), [frontend/src/app/account/page.tsx](frontend/src/app/account/page.tsx) |
| **P2 Wishlist core** (persistence/API, API-backed context, wishlist page/removal) | 🟡 PARTIAL | [backend/Services/WishlistService.cs](backend/Services/WishlistService.cs), [frontend/src/contexts/WishlistContext.tsx](frontend/src/contexts/WishlistContext.tsx), [frontend/src/app/wishlist/page.tsx](frontend/src/app/wishlist/page.tsx); product-card/detail heart controls pending |
| **P2 Development admin bootstrap** (Development only, configurable credentials) | ✅ | [backend/Program.cs](backend/Program.cs), [backend/appsettings.Development.json](backend/appsettings.Development.json) |
| **Global exception handling** (`GlobalExceptionHandler` → ProblemDetails; maps AppException/AuthException/UnauthorizedAccessException) | ✅ | [backend/Services/GlobalExceptionHandler.cs](backend/Services/GlobalExceptionHandler.cs), [backend/Program.cs](backend/Program.cs) |
| **Unified 400 validation format** (RFC 7807 ValidationProblemDetails) | ✅ | `Program.cs` `ConfigureApiBehaviorOptions` |
| Shared AppException (message + HTTP status, factories: NotFound, Conflict, Forbidden) | ✅ | [backend/Services/AppException.cs](backend/Services/AppException.cs) |
| CartService + CartController (full CRUD: get, add with merge + stock check, update qty, remove, clear) | ✅ | [backend/Services/CartService.cs](backend/Services/CartService.cs), [backend/Controllers/CartController.cs](backend/Controllers/CartController.cs) |
| **PaymentService + CheckoutController** (ProcessCheckout, Strategy-pattern Stripe integration, webhook processing, signature verify, event log idempotency) | ✅ | [backend/Services/Payments/](backend/Services/Payments/), [backend/Controllers/CheckoutController.cs](backend/Controllers/CheckoutController.cs) |
| **OrderService + OrdersController** (GetOrders, GetOrderById with user ownership check) | ✅ | [backend/Services/OrderService.cs](backend/Services/OrderService.cs), [backend/Controllers/OrdersController.cs](backend/Controllers/OrdersController.cs) |
| Frontend: products list page (paginated, debounced search, sort dropdown, **server-side category chip filter**, loading skeletons) | ✅ | [frontend/src/app/products/page.tsx](frontend/src/app/products/page.tsx) |
| Frontend: product detail page (dynamic metadata attribute table, stock badges, tags, add-to-cart) | ✅ | [frontend/src/app/products/[id]/page.tsx](frontend/src/app/products/[id]/page.tsx) |
| Frontend: CartContext + useCart (loads on auth, addItem/updateItem/removeItem) | ✅ | [frontend/src/contexts/CartContext.tsx](frontend/src/contexts/CartContext.tsx) |
| Frontend: full cart page (quantity stepper, remove, low stock warning, order summary, Stripe redirect trigger) | ✅ | [frontend/src/app/cart/page.tsx](frontend/src/app/cart/page.tsx) |
| **Frontend: checkout pages** (success transaction detail page, payment cancel redirect page) | ✅ | [frontend/src/app/checkout/](frontend/src/app/checkout/) |
| **Frontend: Order History page** (expandable cards with lazy-loaded item receipts, status badges, frozen pricing) | ✅ | [frontend/src/app/orders/page.tsx](frontend/src/app/orders/page.tsx) |
| Frontend: HeaderNav with user menu, live cart badge count | ✅ | [frontend/src/components/HeaderNav.tsx](frontend/src/components/HeaderNav.tsx) |
| Frontend: shared `Input` component (label/error/helperText/3 sizes) wired into login/register/products | ✅ | [frontend/src/components/ui/Input.tsx](frontend/src/components/ui/Input.tsx) |
| Frontend: token-key bug fixed — `apiCall` 401 path now calls `clearToken()` (clears correct localStorage key + auth cookie) | ✅ | [frontend/src/lib/api.ts](frontend/src/lib/api.ts) |
| Frontend: shared `order.ts` types (de-duplicated from inline interfaces) | ✅ | [frontend/src/types/order.ts](frontend/src/types/order.ts) |
| **Backend Unit Tests** (92 tests across auth, catalogue, cart, payments, admin products/orders, pricing, profile and wishlist) | ✅ 92/92 | [backend.Tests/](backend.Tests/) |
| **Frontend Unit Tests** (12 tests using Vitest for helper formatting/parsing logic) | ✅ | [frontend/src/types/product.test.ts](frontend/src/types/product.test.ts) |
| **Containerized Test Configurations** (`Dockerfile.backend.test`, `Dockerfile.frontend.test`, `vitest.config.ts`) | ✅ | Root & [frontend/vitest.config.ts](frontend/vitest.config.ts) |
| Swagger with Bearer auth button | ✅ | `/swagger` |
| Docker: full stack runs, no host tooling, no port clashes | ✅ | [docker-compose.yml](docker-compose.yml) + local `docker-compose.override.yml` |
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

1. **User Registration & Login** — login/register pages, `useAuth` hook + `AuthContext` (token in localStorage + `novacart_authed` flag cookie for Edge middleware), route guard. ✅
2. **Product Browsing** — 12 seed products / 5 categories, server-side search + category filter + 4 sort modes, dynamic specs table from metadata jsonb. ✅
3. **Shopping Cart** — `Cart`/`CartItem` entities, CartService CRUD with stock boundaries, `CartContext` + cart page. ✅
4. **Checkout & Stripe Payment** — `PaymentMethod`/`Payment`/`PaymentWebhook` entities, Strategy-pattern provider, `ProcessCheckoutAsync` (pending order + Stripe session), `HandleWebhookAsync` (signature verify + idempotent unique index + transactional stock decrement + cart clear). ✅
5. **Order History** — `OrderService` with ownership checks, expandable cards with lazy-loaded receipts and frozen pricing. ✅

---

## 6. P14 requirements coverage map

The P14 spec ([P14_Modern_Ecommerce_Web_App.md](P14_Modern_Ecommerce_Web_App.md)) asks for more than the 5 core MVP features. This maps every requirement to a priority tier.

| P14 requirement | Priority | Status | Notes / where it lands |
|---|---|---|---|
| **Product type-specific attributes** | **P1** | ✅ DONE | Dynamic Specifications table from `Product.Metadata` (jsonb). |
| **Basic search + category + sorting** | **P1** | ✅ DONE | Keyword (ILIKE) + single-category chip + sort by newest/price/name. |
| **Advanced search & filtering** (README #9) | **P2** | 🟡 PARTIAL | Have keyword+one-category+sort. Missing: price-range, tag/attribute **facets**, multi-select category, optional full-text (ER planned GIN/FTS indexes). See P2-12. |
| **RBAC** — 3 roles with access control | **P2** | 🟡 CODE COMPLETE | Roles/claims, protected admin endpoints, frontend role gate and dev admin bootstrap are done. Missing HTTP-level 401/403/admin integration tests. See P2-1. |
| **Customer profile management** | **P2** | ✅ DONE | `GET/PUT /api/users/me` + `/account` edit UI. See P2-2. |
| **Wishlist** | **P2** | 🟡 PARTIAL | Persistence, API, context and wishlist page are done; product-card/detail heart controls remain. See P2-3. |
| **Guest cart + merge on login** | **P2** | 🟡 SCHEMA-READY | `Cart` already has nullable `UserId` + `SessionId`. Missing: merge logic in CartService. See P2-4. |
| **Dynamic pricing / pricing rules** | **P2** | ✅ DONE | `PricingService` rule engine (percent/flat/fixed, product>category>global priority, time windows) wired into products + cart; admin CRUD UI. See P2-5. |
| **Email order confirmation** | **P2** | 🔴 TODO | Background service + SMTP. See P2-6. |
| **Shipping info + delivery status** | **P2** | 🟡 PARTIAL | Order status workflow implemented; shipping address capture at checkout still TODO. See P2-7. |
| **Order status workflow** | **P2** | ✅ DONE | `Order.CurrentStatus` + 6-state state machine + `order_status_history` audit trail + admin transition controller. See P2-7. |
| **Admin dashboard** (product/inventory/order CRUD) | **P2** | ✅ DONE | Product/inventory CRUD + order management with status transitions all done. See P2-8. |
| **Analytics dashboard** | **P2** | 🔴 TODO | ECharts sales dashboard. See P2-9. |
| **PWA** (service worker) | **P2** | 🟡 PARTIAL | `manifest.webmanifest` + icons done. Service Worker missing. See P2-10. |
| Test coverage (components/contexts) | **P2** | 🟡 PARTIAL | Backend: 92/92 service tests. Frontend: 12/12 pure-function tests. Missing HTTP integration and component/context coverage. See P2-11. |

---

## 7. P2 implementation outline (feature by feature)

Legend: 🟢 done · 🟡 partial · 🔴 not started.

> **Ordering note:** P2-1 (RBAC) and P2-8 (Admin dashboard CRUD) are foundational — most other P2
> items (order status workflow, dynamic pricing config, analytics) are exercised *through* admin
> endpoints. P2-1 + P2-8 are now implemented; remaining work should build on them rather than recreate them.

### P2-1 — RBAC (Role-Based Access Control) — 🟡
**Goal:** Enforce the 3 seeded roles (`customer` / `admin` / `sysadmin`) at the endpoint level.
- [x] Roles are seeded and carried as JWT claims.
- [x] All current admin controllers use `[Authorize(Roles = RoleNames.AdminRoles)]` (`admin,sysadmin`).
- [x] Frontend hides admin UI behind `useAuth().user.roles`; middleware protects `/admin/*` from unauthenticated access.
- [x] `apiCall` now distinguishes 401 (clear expired auth) from 403 (show permission error without logging out).
- [x] Development-only admin bootstrap with configurable credentials (`DevBootstrap:*`).
- [ ] Add HTTP authorization tests: unauthenticated 401, customer 403, admin/sysadmin success.

### P2-2 — Customer Profile Management — 🟢
**Goal:** Let customers view and edit their own profile (name, and later address/password).
- [x] `GET /api/users/me` → current user profile DTO.
- [x] `PUT /api/users/me` → update full name (email change/verification deferred).
- [x] `UserService` + `IUserService` in DI; `UsersController`.
- [x] Frontend `/account` edit form using the shared design system.
- [x] 5 `UserServiceTests` covering reads, updates and validation.

### P2-3 — Wishlist — 🟡
**Goal:** Persist a per-user wishlist; toggle from product detail/card.
- [x] `WishlistItem` entity + unique `(UserId, ProductId)` index in the P2 scaffold migration.
- [x] `WishlistService`: get/add/remove with idempotency and inactive-product filtering; authenticated controller endpoints.
- [x] API-backed `WishlistContext` hydration/optimistic toggle and `/wishlist` page with removal.
- [x] 5 `WishlistServiceTests` covering add/remove/dedupe/filtering/error paths.
- [ ] Wire the existing `WishlistContext.toggle` to heart controls on `ProductCard` and product detail; this is the remaining customer-facing gap.

### P2-4 — Guest Cart + Merge on Login — 🔴 (schema-ready)
**Goal:** Anonymous users get a cart keyed by `SessionId`; on login it merges into their user cart.
- The `Cart` entity **already** supports this: nullable `UserId` + `SessionId`. No migration needed.
- [ ] `CartService.GetCartAsync`: resolve by `SessionId` when no user, by `UserId` when authenticated.
- [ ] Identify guests via a `novacart_session` cookie (set in middleware or on first cart action).
- [ ] `MergeGuestCartOnLoginAsync(sessionId, userId)`: copy/sum guest `CartItem` quantities into the user cart, then delete the guest cart.
- [ ] Call merge from the login flow (after JWT issued, before returning).
- [ ] Frontend: load cart by session for guests; after login, refetch.
- [ ] Tests: `CartServiceTests` merge cases (disjoint items, overlapping with quantity sum, empty guest cart).

### P2-5 — Dynamic Pricing / Pricing Rules — 🟢
**Goal:** Admin-configured pricing rules (discount %, flat-off, sale price) applied at price-read time.
- [x] `PriceRule` entity/migration supports product/category/global scope, percent/flat/fixed rules, active windows and enabled state.
- [x] `PricingService` applies most-specific-wins (`product > category > global`), time-window filtering and safe clamping.
- [x] `PriceRuleService` + RBAC admin `GET/POST/DELETE` endpoints and `/admin/pricing` management UI.
- [x] Effective pricing is wired into product list/detail and cart totals; ProductCard/detail show compare-at pricing.
- [x] `OrderItem.PriceAtPurchase` keeps historical order prices frozen.
- [x] 22 pricing/rule tests plus updated product/cart/payment regression coverage.

### P2-6 — Email Order Confirmation — 🔴
**Goal:** Send a confirmation email when an order transitions to Paid (webhook success).
- [ ] Background option: a `BackgroundService` that dequeues "order paid" events, OR send inline in the webhook completion path. Prefer a queue/`Channel<T>` to keep the webhook fast.
- [ ] SMTP integration via `MailKit` (configure host/credentials in `appsettings`).
- [ ] Trigger: inside `PaymentService.ExecutePaymentCompletionAsync` after `order.CurrentStatus = Paid`.
- [ ] Templated HTML email with order number, items, totals.
- [ ] `appsettings` sections: `Smtp:Host`, `:Port`, `:User`, `:Pass`, `:From`.
- [ ] Tests: mock SMTP; assert email fired once per paid order.

### P2-7 — Shipping Info + Order Status Workflow — 🟡
**Goal:** Capture shipping address; let admin advance order status through the full lifecycle.
- [x] Six-state workflow implemented: `pending → paid → processing → shipped → completed`, with cancellation from pending/paid.
- [x] `OrderStatusHistory` entity/table/migration records actor, notes and timestamps.
- [x] RBAC admin list/detail/status endpoints and `/admin/orders` management UI.
- [x] 10 `AdminOrderServiceTests` cover list/detail/legal and illegal transitions, terminals, cancellation and history.
- [ ] `ShippingAddress` value object or entity: `{ OrderId, Line1, Line2, City, State, Postcode, Country }` (or reuse a user Address entity).
- [ ] Capture address at checkout (extend `CheckoutRequest`).
- [ ] Optional: webhook publishes status changes so P2-6 can email shipping updates.
- [ ] Customer order detail still needs shipping address + visible status timeline.

### P2-8 — Admin Dashboard (Product / Inventory / Order CRUD) — 🟢
**Goal:** Admin-facing management surface. **Build alongside P2-1** (RBAC) since these are the protected endpoints.
- [x] `AdminProductsController`: paginated all-status list/detail/categories + `POST`/`PUT`/`DELETE /api/admin/products` (create, edit, reactivate, soft-deactivate).
- [x] Inventory tracking via existing `Product.StockQuantity` (admin can adjust + see low/out-of-stock badges).
- [x] `AdminOrdersController`: list all orders, view, update status through the P2-7 transition service.
- [x] Frontend: `/admin/products` product table, filters, pagination, create/edit form, inventory/status controls.
- [x] Frontend: `/admin/orders` list/filter/detail/status management.
- [x] All current admin endpoints are under `[Authorize(Roles = "admin,sysadmin")]`.
- [x] Service tests: product CRUD, validation, search/filter, inventory and soft-deactivation happy/error paths.
- [ ] HTTP integration tests remain under P2-1/P2-11: admin succeeds; customer receives 403.

### P2-9 — Analytics Dashboard — 🔴
**Goal:** Sales analytics for admins (totals, orders/day, revenue, best-sellers).
- [ ] `AnalyticsService`: aggregate queries over Orders/OrderItems (total sales, orders per day, revenue summary, top products).
- [ ] `GET /api/admin/analytics/summary`, `.../sales-over-time`, `.../best-sellers` (RBAC-guarded).
- [ ] Frontend: ECharts on the `/admin` dashboard (already in the planned stack).
- [ ] Tests: aggregation correctness with seeded orders.

### P2-10 — PWA Service Worker — 🟡 (partial)
**Goal:** Installable, offline-capable PWA.
- `manifest.webmanifest` + icons already done.
- [ ] Add a Service Worker (`sw.js`) for offline shell + cache-first strategy (Next.js: use `next-pwa` or a manual `public/sw.js` registered in `layout.tsx`).
- [ ] Cache static assets + product list; network-first for API.
- [ ] Verify installability + standalone mode (Lighthouse PWA audit).
- [ ] Add to tests: at minimum a build check that `sw.js` is emitted.

### P2-11 — Test Coverage Expansion — 🟡
**Goal:** Beyond pure-function tests, cover components, contexts, and HTTP integration.
- [ ] Frontend: component tests (Vitest + React Testing Library) for `ProductCard`, `Button`, `Input`, cart stepper logic.
- [ ] Frontend: `AuthContext` / `CartContext` integration tests (mock `apiCall`, assert state transitions).
- [ ] Backend: add `WebApplicationFactory`-based integration tests (full HTTP round-trip) for auth + checkout flows — currently tests are service-level only.
- [ ] Raise coverage gating if a CI pipeline is added.

### P2-12 — Advanced Search & Filtering — 🟡 (partial; README #9)
**Goal:** Go beyond the P1 keyword+single-category+sort to true faceted filtering, as the P14 spec's
"multi-category search with type-based filtering and sorting" requires.
- Today: `ProductService` does ILIKE keyword + one `categoryId` + sort. This item adds facets.
- [ ] **Price-range filter** (`minPrice`/`maxPrice`) in the products query + a range control in the filter rail.
- [ ] **Tag facets** — filter by `Product.Tags` (Postgres `text[]`, `= ANY`); surface available tags as multi-select chips. Add the GIN index the ER planned (`idx_products_tags_gin`).
- [ ] **Multi-select category** (accept `categoryId` list) instead of a single chip.
- [ ] *(Optional)* **Full-text search** on name+description via a Postgres `tsvector` GIN index (ER planned `idx_products_fts`) — replaces ILIKE for relevance/ranking.
- [ ] Frontend: expand the filter rail (price slider, tag chips, multi-category); reflect active facets as removable chips; keep it URL-driven (query params) so results are shareable/back-button-safe.
- [ ] Tests: `ProductServiceTests` for price-range bounds, tag ANY-match, combined facets.

---

## 8. P2 execution plan, current progress & TODO

This is the recommended continuation plan. Work in **vertical slices**: database/model → service → API → UI → Docker tests. Do not fill every stub in parallel.

### Stop point and working-tree warning

- The working tree intentionally contains the user's repository-hygiene changes plus the expanded P2 A/B/C/D implementation. **Do not reset, clean, or discard them.**
- User-owned cleanup includes `.gitignore`, root/per-project `.dockerignore` files, `Dockerfile.frontend.test`, `frontend/package-lock.json`, and staged removal of previously tracked `backend.Tests/bin` / `obj` artefacts.
- P2 work is currently uncommitted: admin product/order services and pages, order-status entity/migration, pricing engine/rule CRUD, profile, wishlist, DI/config changes and their tests.
- `docker compose up -d` succeeded and recreated the Novacart stack. The subsequent in-container HTTP/browser acceptance attempt hung and was aborted; no result from that attempt should be treated as verified.
- Browser access to localhost was blocked by the in-app browser policy, so visual/authenticated E2E was not completed.

### Verification record — exact, do not overstate

- [x] Backend Docker suite passed **92/92** — includes admin product (9), admin order (10), pricing (14+8), wishlist (5), profile (5) tests.
- [x] Frontend Docker suite passed **12/12**.
- [x] `docker compose build backend frontend` succeeded; Next generates all 20 routes and all admin/account/wishlist pages pass TypeScript/build checks.
- [x] EF migration `AddOrderStatusWorkflow` generated (adds `orders.UpdatedAt` + `order_status_history` table).
- [ ] Authenticated runtime acceptance: order status transitions, pricing rule effects, wishlist/profile CRUD as logged-in users.
- [ ] RBAC HTTP acceptance: unauthenticated → 401, customer → 403, admin/sysadmin → success. **Requires WebApplicationFactory integration tests** (not yet set up — needs `Microsoft.AspNetCore.Mvc.Testing` + InMemory DB swap).

### P2-0 — engineering baseline

- [x] Generalise Git ignores so all .NET `bin/obj` output is excluded.
- [x] Add root `.dockerignore`; test build context dropped from roughly 362 MB to a few hundred KB.
- [x] Make the frontend test image use `package-lock.json` + `npm ci`.
- [x] Removed 210 tracked `backend.Tests/bin`+`obj` build artifacts from Git index (kept on disk).
- [ ] Stage and commit the repository-hygiene and P2 changes in sensible separate commits if desired.
- [ ] Upgrade vulnerable Next.js/dependencies. Current Docker install reports 6 audit findings (3 moderate, 1 high, 2 critical); choose a patched compatible version and verify via Docker.
- [ ] Add early Docker CI: backend tests, frontend tests, backend Release publish, frontend production build.

### P2-A — RBAC + admin catalogue foundation ✅

- [x] P2-1 core RBAC: role claims/constants, `[Authorize(Roles=...)]`, frontend admin role gate, middleware `/admin/*` protection.
- [x] Admin product DTOs with validation for name, slug, price, currency, stock and metadata size.
- [x] Admin product service: include active/inactive products, name/slug search, status filter, pagination, categories, create, update, stock adjustment, reactivate and soft-deactivate.
- [x] Admin product API: `GET` list/detail/categories, `POST`, `PUT`, `DELETE` under RBAC.
- [x] `/admin/products`: table, search/status filters, pagination, stock badges, create/edit form, metadata JSON validation and deactivate confirmation.
- [x] Shared API wrapper: distinguish 401 from 403, surface ProblemDetails/validation messages, support 204 responses (PATCH method now supported).
- [x] Add 9 `AdminProductServiceTests`.
- [x] **Dev admin bootstrap** in `Program.cs` — seeds an admin account on Development startup (configurable via `DevBootstrap:*` in `appsettings.Development.json`; never runs in production). Default: `admin@novacart.local` / `Admin123!`.
- [ ] Add `WebApplicationFactory` RBAC/integration tests for 401/403/admin success (requires InMemory DB swap — significant setup).
- [ ] Perform authenticated browser/API acceptance.

### P2-B — order status workflow + order management ✅

- [x] `OrderStatusHistory` entity + `order_status_history` table + migration `AddOrderStatusWorkflow`.
- [x] `AdminOrderService`: paginated list (search by order #/email, filter by status), detail with line items, status transition validation (state machine: pending→paid→processing→shipped→completed + cancelled from pending/paid).
- [x] `AdminOrdersController`: `GET` list, `GET` detail, `PATCH {id}/status` — all RBAC-guarded.
- [x] `/admin/orders`: table with search/status filter, detail modal with items + totals, advance-status + cancel buttons.
- [x] 10 `AdminOrderServiceTests` (list, detail, legal transitions, illegal transitions, unknown status, terminal status, cancellation, audit history).
- [ ] Shipping address capture at checkout still TODO (P2-7 partial).

### P2-C — dynamic pricing ✅

- [x] `PricingService`: pure rule-evaluation engine — percent/flat/fixed, product>category>global scope priority, time-window filtering, percent clamping (0–100), negative-price clamp at 0.
- [x] `PriceRuleService`: admin CRUD (list, create with validation, delete) for price rules.
- [x] `AdminPriceRulesController`: `GET`, `POST`, `DELETE` under RBAC.
- [x] Wired `IPricingService` into `ProductService` (catalog list + detail) and `CartService` (unit prices, line totals, subtotal) — dynamic pricing now flows through to cart.
- [x] `/admin/pricing`: rule table (scope, type, value, time window, status) + create form (global/category/product scope, rule type, value, dates, active toggle).
- [x] 22 `PricingServiceTests` + `PriceRuleServiceTests` (scope priority, time windows, clamping, CRUD validation).
- [x] `OrderItem.PriceAtPurchase` snapshot unaffected (orders stay frozen — verified by design).

### P2-D — customer profile complete; wishlist core partial 🟡

- [x] **P2-2 Profile:** `UserService` (`GET/PUT /api/users/me`), name editing + validation, `/account` page with edit form (email read-only, verification deferred). 5 `UserServiceTests`.
- [x] **P2-3 Wishlist:** `WishlistService` (get/add/remove, idempotent, inactive-product filtering), API-backed `WishlistContext` (hydrates on auth, optimistic toggle), `/wishlist` page with remove. 5 `WishlistServiceTests`.
- [ ] Add wishlist heart/toggle controls to `ProductCard` and product detail so customers can add items through the visible UI.
- [ ] **P2-4 Guest cart:** session cookie + merge-on-login logic still TODO (schema is ready).
- [ ] **P2-12 Advanced search:** price-range, tag facets, multi-category still TODO.

### P2-E — operations and completion

- [ ] **P2-7 Shipping:** customer address CRUD, checkout selection/capture, frozen order address snapshot and customer order timeline.
- [ ] **P2-3 Wishlist UI completion:** product-card/detail heart controls.
- [ ] **P2-4 Guest cart:** session cookie, anonymous cart resolution, merge-on-login and stock-conflict handling.
- [ ] **P2-12 Advanced search:** price range, multi-category and tag/attribute facets with URL-driven filters and GIN indexes.
- [ ] **P2-9 Analytics:** revenue/order summaries, sales over time, best sellers and low-stock data; implement admin charts/dashboard.
- [ ] **P2-6 Email:** send paid/shipped/cancelled notifications through a background queue/service so webhook processing is not blocked.
- [ ] **P2-10 PWA:** Service Worker, offline shell/static caching and installability/Lighthouse verification.
- [ ] **P2-11 Testing:** `WebApplicationFactory` RBAC/API tests plus React Testing Library component/context coverage.
- [ ] Add coverage reporting/gates to CI.

### Recommended next action for the next AI

1. Read this file and preserve the dirty worktree.
2. Run the two Docker test commands in §2; confirm backend **92/92** and frontend **12/12**.
3. Inspect the existing P2 A/B/C/D diff; do not reimplement admin products/orders, pricing, profile or wishlist persistence.
4. Add `WebApplicationFactory` RBAC/API tests and perform authenticated acceptance of the completed slices.
5. Continue with one remaining vertical slice, preferably **shipping/address capture** or **guest-cart merge**.

---

## 9. Tech-debt / housekeeping notes (non-blocking, opportunistic)

- `AuthException` is a separate class from `AppException` with an identical shape (message + `StatusCode`). They could be unified (`AuthException : AppException`) for simplicity, but both are already handled by `GlobalExceptionHandler`, so this is cosmetic.
- `docker-compose.override.yml` is intentionally not committed (machine-local port remap to avoid clashes with another stack on 3000/5432/6379).
- `vitest.config.ts` sits in the frontend tsconfig `include`, so `next build` type-checks it — harmless because `vitest` is a devDependency, but it means a fresh clone must `npm install` (dev deps included) before `next build`. Consider excluding test config from the production tsconfig if build time matters.

---

## 10. P3 — Technical Enhancements (implementation outline)

Maps to the README's **Priority 3** tier (#10–#13). Several P3 items are already partially satisfied by P1 work;
this section records what's left. Do P3 **after** the P2 features, except CI/CD (P3-1) which is worth standing up early.

**P3 tier — already satisfied by P1:**

| README P3 item | Status |
|---|---|
| Reusable components + custom hooks (#10) | ✅ Mostly done (`useAuth`/`useCart`, `Button`/`Card`/`Input`/`Badge`/`ProductCard`/`EmptyState`, `HeaderNav`). |
| Unified API layer + Swagger (#11) | ✅ Done (`apiCall` wrapper with token/401 handling; Swashbuckle + Bearer). |
| Layered architecture + Strategy pattern (#13) | ✅ Done (Controller→Service→Entity; `IPaymentStrategy`). |

### P3-1 — CI/CD pipeline (GitHub Actions) — 🔴
**Goal:** Automated build + test on every push/PR; optional deploy.
- [ ] Workflow: build backend + run xUnit; build frontend + run Vitest — reuse the containerized `Dockerfile.*.test`.
- [ ] Cache NuGet + npm for speed; run on PR + `main`.
- [ ] Status badges in the README; branch protection requiring green.
- [ ] *(Optional)* deploy job (build+push images; deploy to the target env — see P3-6).

### P3-2 — Test coverage expansion — 🔴 (continues P2-11)
**Goal:** Move beyond pure-function tests to components, contexts, and HTTP integration.
- [ ] Frontend: RTL component tests (`ProductCard`, `Button`, `Input`, cart stepper) + `AuthContext`/`CartContext` integration tests (mock `apiCall`).
- [ ] Backend: `WebApplicationFactory` integration tests (full HTTP round-trip) for auth + checkout.
- [ ] Coverage reporting + a gate in the P3-1 pipeline.

### P3-3 — Code quality & patterns (#13) — 🔴
**Goal:** Round out the "engineering depth" requirements.
- [ ] **Factory pattern** where it fits (e.g. an `OrderFactory` building an `Order`+`OrderItems`+snapshot from a cart; or a `PaymentStrategyFactory` if strategy selection grows). Document the two patterns (Factory + existing Strategy).
- [ ] **Mapper layer** — extract entity→DTO mapping into `Mappers/` (or Mapster/AutoMapper) as DTOs multiply, completing the `Controller → Service → Mapper → Entity` layering named in the README.
- [ ] **Alibaba DB standards pass** — audit naming/index/`BIGINT`/`DECIMAL` conventions against the standard; document deviations (we use Guid PKs by design).

### P3-4 — Frontend architecture polish (#10) — 🔴
**Goal:** The remaining items from the README's frontend-architecture list.
- [ ] **Responsive sidebar** for the `/admin` area (auto-collapse ≤768px) — pairs with P2-8.
- [ ] Extract more shared primitives as pages grow (e.g. `DataTable`, `Pagination`, `Modal`, `Toast`).
- [ ] Nested routing/route-guard refinement (customer vs admin layouts; loading/error boundaries per segment).
- [ ] A11y + responsive audit across breakpoints (mobile/tablet/desktop) per the P14 non-functional reqs.

### P3-5 — Performance & caching — 🔴
**Goal:** Use the Redis that's already wired but currently idle.
- [ ] **Redis cache** for the product list and recent orders (README P1 mentioned Redis-cached recent orders; not yet implemented — pick it up here). Cache-aside with sensible TTLs + invalidation on writes.
- [ ] Response compression, HTTP caching headers for static/product responses.
- [ ] DB: confirm the ER's planned indexes exist for hot queries (orders by user+status, product filters).

### P3-6 — Deployment & ops — 🔴
**Goal:** Get it running somewhere real (README targets AWS).
- [ ] `docker-compose.prod.yml` (already referenced in the README) with production settings + secrets via env, not appsettings.
- [ ] Move `Jwt:Secret`, Stripe keys, DB creds to real secret storage; drop dev placeholders.
- [ ] AWS path per README: EC2 (app), RDS (Postgres), ElastiCache (Redis), S3 (product images / static).
- [ ] Structured logging + a deeper `/health` (DB + Redis readiness); basic error monitoring.

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
backend **92/92** Docker tests, frontend **12/12** Docker tests, and successful backend/frontend production builds
(20 frontend routes). HTTP-level RBAC tests and authenticated browser acceptance remain open.

**Backend implementation**

| Piece | File | Status / maps to |
|---|---|---|
| P2 entities + DbSets | `WishlistItem.cs`, `PriceRule.cs`, `UserAddress.cs`, `OrderStatusHistory.cs`, `AppDbContext.cs` | P2-3 / P2-5 / P2-7 implemented schema |
| Migrations | `AddP2Scaffold`, `AddOrderStatusWorkflow` | Wishlist/pricing/address tables + order status history |
| RBAC + development admin | `Role.cs`, `Program.cs`, `appsettings.Development.json` | Core complete; HTTP integration tests pending |
| Admin products | `AdminProductService.cs`, `AdminProductsController.cs` | Implemented |
| Admin orders/status workflow | `AdminOrderService.cs`, `AdminOrdersController.cs` | Implemented |
| Dynamic pricing/rules | `PricingService.cs`, `PriceRuleService.cs`, `AdminPriceRulesController.cs` | Implemented |
| Profile | `UserService.cs`, `UsersController.cs` | Implemented |
| Wishlist | `WishlistService.cs`, `WishlistController.cs` | Persistence/API implemented |
| Analytics | `AnalyticsService.cs`, `AdminAnalyticsController.cs` | Remaining 501 stub |
| Guest cart, shipping capture, email | Cart/checkout/payment services | Remaining P2 work; see §8 |

**Frontend implementation**

| Piece | File | Status / maps to |
|---|---|---|
| Admin shell + guards | `app/admin/layout.tsx`, `middleware.ts` | Implemented |
| Product/inventory management | `app/admin/products/page.tsx` | Implemented |
| Order/status management | `app/admin/orders/page.tsx` | Implemented |
| Pricing-rule management | `app/admin/pricing/page.tsx` | Implemented |
| Profile | `app/account/page.tsx` | Implemented |
| Wishlist persistence/UI | `contexts/WishlistContext.tsx`, `app/wishlist/page.tsx` | Core implemented; product-card/detail heart controls pending |
| Dashboard/analytics | `app/admin/page.tsx`, `app/admin/analytics/page.tsx` | Remaining `ComingSoon` pages |
| Guest cart, shipping timeline, advanced filters, PWA | Customer-facing routes/components | Remaining P2 work; see §8 |

**Continuation rules:**
1. Preserve the dirty worktree; these P2 implementations are not committed on `main` yet.
2. Use Docker for all builds/tests. First reconfirm **92/92** backend and **12/12** frontend.
3. Do not recreate admin products/orders, pricing, profile or wishlist persistence.
4. Choose one remaining vertical slice from §8 and finish it end-to-end with tests.
