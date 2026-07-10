# Novacart — Handoff & Roadmap

> **Purpose of this file.** A self-contained handoff so another AI/developer can continue building or
> auditing Novacart without re-discovering context. It records what is already done, how to run the
> project, the conventions to follow, the frontend design system to build against, and the
> **Priority 2 (P2)** feature-by-feature implementation plan with a flat checklist.
>
> **Status:** Priority 1 (MVP) is **complete & verified**. This file now orients toward **Priority 2**.
>
> Last updated: 2026-07-10 — P1 complete & independently re-verified (backend build + 41 tests pass, frontend build + 12 tests pass); P2 plan reconciled with README/P14 (added P2-12 Advanced Search & Filtering + a P3/planned roadmap tail in §10).

---

## 0. TL;DR for whoever picks this up

- **Stack:** Backend ASP.NET Core 8 + EF Core + PostgreSQL + Redis. Frontend Next.js 14 (App Router) + TS + Tailwind.
- **Priority 1 (MVP) is 100% completed and verified** — the 5 core features (auth, products, cart, checkout, orders) all work end-to-end.
- **P1 polish pass done:** global exception handling middleware (controllers no longer need try/catch), token-key bug fixed, category filter wired to backend, shared `Input` component, unified 400 validation format, type de-duplication.
- **Priority 2 is the current focus.** The schema is already future-proofed for most P2 features (6-state order workflow, guest-cart columns) — see §7.
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
| GetEffectivePrice / ResolvePrice seam (P1 = pass-through, ready for P2 dynamic pricing) | ✅ | `ProductService.ResolvePrice` (static) |
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
| **Backend Unit Tests** (41 tests using xUnit & InMemory DB for Auth, Products, Cart, Orders, and Payments) | ✅ | [backend.Tests/](backend.Tests/) |
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

**Backend Tests (41 cases for Auth, Products, Cart, Orders, and Payments logic):**
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
| **RBAC** — 3 roles with access control | **P2** | 🔴 TODO | Roles seeded + claims in JWT. Missing: `[Authorize(Roles=...)]` on admin endpoints + a 403 path. See P2-1. |
| **Customer profile management** | **P2** | 🔴 TODO | `GET/PUT /api/users/me` for profile edit. See P2-2. |
| **Wishlist** | **P2** | 🔴 TODO | `Wishlist` entity (user_id, product_id, added_at) + UI toggle. See P2-3. |
| **Guest cart + merge on login** | **P2** | 🟡 SCHEMA-READY | `Cart` already has nullable `UserId` + `SessionId`. Missing: merge logic in CartService. See P2-4. |
| **Dynamic pricing / pricing rules** | **P2** | 🟡 SEAM-READY | `ProductService.ResolvePrice` is a static pass-through today. P2 replaces the body with rule-based logic. See P2-5. |
| **Email order confirmation** | **P2** | 🔴 TODO | Background service + SMTP. See P2-6. |
| **Shipping info + delivery status** | **P2** | 🔴 TODO | Address capture + admin delivery updates. See P2-7. |
| **Order status workflow** | **P2** | 🟡 SCHEMA-READY | `Order.CurrentStatus` + full 6-state enum (`OrderStatuses`) in place. Missing admin transition controller. See P2-7. |
| **Admin dashboard** (product/inventory/order CRUD) | **P2** | 🔴 TODO | See P2-8. |
| **Analytics dashboard** | **P2** | 🔴 TODO | ECharts sales dashboard. See P2-9. |
| **PWA** (service worker) | **P2** | 🟡 PARTIAL | `manifest.webmanifest` + icons done. Service Worker missing. See P2-10. |
| Test coverage (components/contexts) | **P2** | 🔴 TODO | Only pure-function tests exist (41 BE / 12 FE). See P2-11. |

---

## 7. P2 implementation outline (feature by feature)

Legend: 🟢 done · 🟡 partial · 🔴 not started. **All 🔴 below.**

> **Ordering note:** P2-1 (RBAC) and P2-8 (Admin dashboard CRUD) are foundational — most other P2
> items (order status workflow, dynamic pricing config, analytics) are exercised *through* admin
> endpoints. Build P2-1 + P2-8 first.

### P2-1 — RBAC (Role-Based Access Control) — 🔴
**Goal:** Enforce the 3 seeded roles (`customer` / `admin` / `sysadmin`) at the endpoint level.
- Roles are already seeded and carried as JWT claims. What's missing is enforcement.
- [ ] Add `[Authorize(Roles = "admin,sysadmin")]` to all admin endpoints created in P2-8.
- [ ] Verify the `GlobalExceptionHandler` returns 403 for `AppException.Forbidden()` (it does — confirm by test).
- [ ] Frontend: hide admin UI behind a role check (`useAuth().user.roles`); gate `/admin/*` routes in `middleware.ts`.
- [ ] Seed a second test user with `admin` role for manual verification.
- [ ] Tests: add an authorization test fixture (authenticated-as-admin vs customer asserting 403).

### P2-2 — Customer Profile Management — 🔴
**Goal:** Let customers view and edit their own profile (name, and later address/password).
- [ ] `GET /api/users/me` → current user profile DTO.
- [ ] `PUT /api/users/me` → update full name (email change needs a verification flow — defer).
- [ ] `UserService` + `IUserService` in DI; `UsersController`.
- [ ] Frontend: `/account` (or `/profile`) page with the shared `Input` component + a form.
- [ ] Tests: `UserServiceTests` (update persists, rejects invalid input).

### P2-3 — Wishlist — 🔴
**Goal:** Persist a per-user wishlist; toggle from product detail/card.
- [ ] `Wishlist` entity: `{ Id, UserId, ProductId, AddedAt }` + unique index on `(UserId, ProductId)`.
- [ ] Migration `AddWishlist`.
- [ ] `WishlistService`: `GetAsync`, `AddAsync`, `RemoveAsync`; `WishlistController` (`GET /api/wishlist`, `POST /api/wishlist/items`, `DELETE /api/wishlist/items/{productId}`).
- [ ] Frontend: `WishlistContext` + `/wishlist` page + heart toggle on `ProductCard` / product detail.
- [ ] Tests: `WishlistServiceTests` (add, remove, dedupe, ownership).

### P2-4 — Guest Cart + Merge on Login — 🔴 (schema-ready)
**Goal:** Anonymous users get a cart keyed by `SessionId`; on login it merges into their user cart.
- The `Cart` entity **already** supports this: nullable `UserId` + `SessionId`. No migration needed.
- [ ] `CartService.GetCartAsync`: resolve by `SessionId` when no user, by `UserId` when authenticated.
- [ ] Identify guests via a `novacart_session` cookie (set in middleware or on first cart action).
- [ ] `MergeGuestCartOnLoginAsync(sessionId, userId)`: copy/sum guest `CartItem` quantities into the user cart, then delete the guest cart.
- [ ] Call merge from the login flow (after JWT issued, before returning).
- [ ] Frontend: load cart by session for guests; after login, refetch.
- [ ] Tests: `CartServiceTests` merge cases (disjoint items, overlapping with quantity sum, empty guest cart).

### P2-5 — Dynamic Pricing / Pricing Rules — 🔴 (seam-ready)
**Goal:** Admin-configured pricing rules (discount %, flat-off, sale price) applied at price-read time.
- **The seam already exists:** `ProductService.ResolvePrice(Product)` is the single chokepoint. Today it returns `product.Price` verbatim.
- [ ] New entities: `PriceRule { Id, ProductId?, CategoryId?, RuleType (percent/flat/fixed), Value, StartsAt, EndsAt, IsActive }`.
- [ ] Migration `AddPriceRules`.
- [ ] Replace `ResolvePrice` body: load applicable active rules and compute effective price. Keep it **static-safe** for EF LINQ (pre-materialise, then apply) — or move rule resolution into the service query path.
- [ ] Admin endpoints to CRUD price rules (P2-8).
- [ ] Display effective vs original price (strikethrough) on ProductCard / detail.
- [ ] **Order snapshot stays frozen** — `OrderItem.PriceAtPurchase` already snapshots at checkout, so historical orders are unaffected. Verify this.
- [ ] Tests: `ProductServiceTests` for rule application (percent, expired rule ignored, most-specific wins).

### P2-6 — Email Order Confirmation — 🔴
**Goal:** Send a confirmation email when an order transitions to Paid (webhook success).
- [ ] Background option: a `BackgroundService` that dequeues "order paid" events, OR send inline in the webhook completion path. Prefer a queue/`Channel<T>` to keep the webhook fast.
- [ ] SMTP integration via `MailKit` (configure host/credentials in `appsettings`).
- [ ] Trigger: inside `PaymentService.ExecutePaymentCompletionAsync` after `order.CurrentStatus = Paid`.
- [ ] Templated HTML email with order number, items, totals.
- [ ] `appsettings` sections: `Smtp:Host`, `:Port`, `:User`, `:Pass`, `:From`.
- [ ] Tests: mock SMTP; assert email fired once per paid order.

### P2-7 — Shipping Info + Order Status Workflow — 🔴 (schema-ready)
**Goal:** Capture shipping address; let admin advance order status through the full lifecycle.
- `OrderStatuses` **already** defines the full P14 workflow: `pending → paid → processing → shipped → completed (+ cancelled)`.
- **Design choice:** the [ER doc](Database_ER_Diagram.md) designed a *table-driven* state machine (`order_status_transitions` seed + `order_status_history` audit trail). Simplest P2 path is a hardcoded allowed-transition map + validation; adopt the history table if you want a full admin audit trail (recommended for the admin dashboard).
- [ ] `ShippingAddress` value object or entity: `{ OrderId, Line1, Line2, City, State, Postcode, Country }` (or reuse a user Address entity).
- [ ] Capture address at checkout (extend `CheckoutRequest`).
- [ ] Admin order-status controller: `PATCH /api/admin/orders/{id}/status` (guarded by P2-1 RBAC) with allowed-transition validation.
- [ ] Optional: webhook publishes status changes so P2-6 can email shipping updates.
- [ ] Frontend: order detail shows shipping address + current status timeline; admin can advance status.
- [ ] Tests: transition validation (illegal jumps rejected), ownership checks.

### P2-8 — Admin Dashboard (Product / Inventory / Order CRUD) — 🔴
**Goal:** Admin-facing management surface. **Build alongside P2-1** (RBAC) since these are the protected endpoints.
- [ ] `AdminProductsController`: `POST`/`PUT`/`DELETE /api/admin/products` (create, edit price/stock/description, deactivate).
- [ ] Inventory tracking via existing `Product.StockQuantity` (admin can adjust + see low-stock).
- [ ] `AdminOrdersController`: list all orders, view, update status (calls P2-7 transition logic).
- [ ] Frontend: `/admin` section — product table (CRUD), inventory view, order management. Use the design system.
- [ ] All endpoints under `[Authorize(Roles = "admin,sysadmin")]`.
- [ ] Tests: admin-only access (403 for customers), CRUD happy paths.

### P2-9 — Analytics Dashboard — 🔴
**Goal:** Sales analytics for admins (totals, orders/day, revenue, best-sellers).
- [ ] `AnalyticsService`: aggregate queries over Orders/OrderItems (total sales, orders per day, revenue summary, top products).
- [ ] `GET /api/admin/analytics/summary`, `.../sales-over-time`, `.../best-sellers` (RBAC-guarded).
- [ ] Frontend: ECharts on the `/admin` dashboard (already in the planned stack).
- [ ] Tests: aggregation correctness with seeded orders.

### P2-10 — PWA Service Worker — 🔴 (partial)
**Goal:** Installable, offline-capable PWA.
- `manifest.webmanifest` + icons already done.
- [ ] Add a Service Worker (`sw.js`) for offline shell + cache-first strategy (Next.js: use `next-pwa` or a manual `public/sw.js` registered in `layout.tsx`).
- [ ] Cache static assets + product list; network-first for API.
- [ ] Verify installability + standalone mode (Lighthouse PWA audit).
- [ ] Add to tests: at minimum a build check that `sw.js` is emitted.

### P2-11 — Test Coverage Expansion — 🔴
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

## 8. Flat P2 TODO checklist

**Cross-cutting / foundational (do these first)**
- [ ] P2-1 RBAC: `[Authorize(Roles=...)]` on admin endpoints + frontend role gating + middleware `/admin/*` guard.
- [ ] P2-8 Admin CRUD endpoints + frontend `/admin` section (built together with P2-1).
- [ ] Frontend: add an `admin` seed user for manual verification.

**Features**
- [ ] P2-2 Profile: `UsersController` `GET/PUT /api/users/me` + `/account` page.
- [ ] P2-3 Wishlist: `Wishlist` entity + migration + service/controller + `WishlistContext` + `/wishlist` page + heart toggle.
- [ ] P2-4 Guest cart: session cookie + `CartService` session resolution + `MergeGuestCartOnLoginAsync`.
- [ ] P2-5 Dynamic pricing: `PriceRule` entity + migration + `ResolvePrice` body + admin CRUD + strikethrough UI.
- [ ] P2-6 Email: background sender + SMTP config + trigger on Paid + template.
- [ ] P2-7 Shipping + status workflow: address capture + `PATCH /api/admin/orders/{id}/status` + status timeline UI.
- [ ] P2-9 Analytics: `AnalyticsService` + endpoints + ECharts dashboard.
- [ ] P2-10 PWA: Service Worker + offline shell + Lighthouse pass.
- [ ] P2-12 Advanced search: price-range + tag facets + multi-category (+ optional FTS) with GIN indexes and a URL-driven filter rail.

**Quality**
- [ ] P2-11 Tests: frontend component/context tests (RTL) + backend `WebApplicationFactory` integration tests.
- [ ] Optional: CI pipeline (GitHub Actions) running both test suites containerized.

---

## 9. Tech-debt / housekeeping notes (non-blocking, opportunistic)

- `AuthException` is a separate class from `AppException` with an identical shape (message + `StatusCode`). They could be unified (`AuthException : AppException`) for simplicity, but both are already handled by `GlobalExceptionHandler`, so this is cosmetic.
- `docker-compose.override.yml` is intentionally not committed (machine-local port remap to avoid clashes with another stack on 3000/5432/6379).
- `vitest.config.ts` sits in the frontend tsconfig `include`, so `next build` type-checks it — harmless because `vitest` is a devDependency, but it means a fresh clone must `npm install` (dev deps included) before `next build`. Consider excluding test config from the production tsconfig if build time matters.
- This file is still named `HANDOFF_P1.md` but now covers the whole roadmap — consider renaming to `HANDOFF.md`.

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

**P2 (by dependency):** **P2-1 RBAC + P2-8 Admin CRUD together** (foundation) → P2-7 order-status workflow →
P2-5 dynamic pricing (needs admin CRUD) → P2-9 analytics (needs orders/admin) → P2-3 wishlist / P2-2 profile /
P2-4 guest-cart / P2-12 advanced search (independent, any order) → P2-6 email → P2-10 PWA SW → P2-11 tests (continuous).

**P3 (after P2, except CI early):** P3-1 CI/CD (stand up early) → P3-3 patterns/mappers + P3-4 FE polish (alongside admin work) →
P3-5 caching → P3-2 deeper tests → P3-6 deployment.

> **Scaffold note:** a compiling **P2 skeleton** has been committed (see §13) — entities, service/controller stubs,
> RBAC policy, and admin/account/wishlist frontend shells — so P2 work is "fill in the bodies," not "start from zero."

---

## 13. P2 scaffold — what's already wired (fill in the bodies)

A **compiling, non-breaking P2 skeleton** is in place. Everything below builds; backend 41 tests still pass; frontend
builds (20 routes). Stub endpoints return **501 Not Implemented** (via `AppException.NotImplemented`) and stub services
throw the same — so nothing lies about being done, and **RBAC is real** (customers get **403** on admin routes today).
Implementing a P2 feature = replace the stub body; the entity, DI, route, and auth wiring already exist.

**Backend scaffold**

| Piece | File | Maps to |
|---|---|---|
| Entities + DbSets + config | `Models/Entities/WishlistItem.cs`, `PriceRule.cs`, `UserAddress.cs`; wired in [AppDbContext.cs](backend/Data/AppDbContext.cs) | P2-3 / P2-5 / P2-7 |
| Migration | `Data/Migrations/*_AddP2Scaffold.cs` (creates `wishlist_items`, `price_rules`, `user_addresses`) | — |
| RBAC constant | `RoleNames.AdminRoles` (`"admin,sysadmin"`) in [Role.cs](backend/Models/Entities/Role.cs) | P2-1 |
| Stub services (DI-registered in [Program.cs](backend/Program.cs)) | `Services/UserService.cs`, `WishlistService.cs`, `PricingService.cs` (pass-through), `AnalyticsService.cs` | P2-2 / P2-3 / P2-5 / P2-9 |
| Customer controllers | `Controllers/UsersController.cs`, `WishlistController.cs` (`[Authorize]`) | P2-2 / P2-3 |
| Admin controllers (`[Authorize(Roles = RoleNames.AdminRoles)]`) | `Controllers/Admin/AdminProductsController.cs`, `AdminOrdersController.cs`, `AdminPriceRulesController.cs`, `AdminAnalyticsController.cs` | P2-8 / P2-7 / P2-5 / P2-9 |
| 501 helper | `AppException.NotImplemented()` in [AppException.cs](backend/Services/AppException.cs) | — |

**Frontend scaffold**

| Piece | File | Maps to |
|---|---|---|
| Admin shell (sidebar + client-side role gate) | `app/admin/layout.tsx` | P2-1 / P2-8 |
| Admin stub pages | `app/admin/page.tsx`, `admin/products`, `admin/orders`, `admin/pricing`, `admin/analytics` | P2-5/7/8/9 |
| Account (profile, read-only) | `app/account/page.tsx` | P2-2 |
| Wishlist page + context | `app/wishlist/page.tsx`, `contexts/WishlistContext.tsx` (local-only) | P2-3 |
| `ComingSoon` placeholder | `components/ui/ComingSoon.tsx` | — |
| Route guards | `middleware.ts` now protects `/admin`, `/account`, `/wishlist` | P2-1 |
| Nav discoverability | `HeaderNav.tsx` user menu → Account / Wishlist / Orders (+ Admin for admins) | — |

**To start building on the scaffold:**
1. `docker compose up --build -d` — auto-migration creates the new tables on backend startup.
2. **Seed an admin user** to exercise the admin area — assign `RoleNames.AdminId` in `user_roles` (or add a dev seed). Without one, `/admin` correctly shows "access required".
3. Pick a P2 item, replace the stub body (service + controller), and delete the `NotImplemented`/`ComingSoon` placeholder. Wiring, DI, routes, and RBAC are already there.

> Note: the running Docker stack was built before this scaffold — rebuild (`--build`) to apply the `AddP2Scaffold` migration.
