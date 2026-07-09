# Novacart — P1 (MVP) Handoff & TODO

> **Purpose of this file.** A self-contained handoff so another AI/developer can continue building or auditing
> the Priority-1 MVP without re-discovering context. It records what is already done, how to run the
> project, the conventions to follow, the frontend design system to build against, and a
> feature-by-feature implementation plan with a flat checklist.
>
> Last updated: 2026-07-09 (Session 3 - Complete). Scope: **Priority 1 — MVP Core Features** completed.

---

## 0. TL;DR for whoever picks this up

- Backend: ASP.NET Core 8 + EF Core + PostgreSQL + Redis. Frontend: Next.js 14 (App Router) + TS + Tailwind.
- **Priority 1 (MVP) is 100% completed and verified.**
- **Auth (register / login / JWT)**: Complete frontend & backend flows. Next.js middleware guards protected routes.
- **Product browsing**: DB-backed list with paginated search/filter/sorting. Details page displays dynamic specs table from `metadata` jsonb.
- **Shopping cart**: Full CRUD with stock validation, React CartContext, and cart page.
- **Checkout & Stripe Payment**: Wires cart to Stripe Checkout redirect. Webhook handles signature verification, webhook idempotency log, order/payment transition, inventory decrement, and cart clearing.
- **Order history**: Expansion card displays historical purchases with frozen snapshotted pricing.
- **Testing**: 41 backend tests (xUnit) and 12 frontend tests (Vitest) all running containerized in Docker.
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
| Shared AppException (message + HTTP status, factories: NotFound, Conflict, Forbidden) | ✅ | [backend/Services/AppException.cs](backend/Services/AppException.cs) |
| CartService + CartController (full CRUD: get, add with merge + stock check, update qty, remove, clear) | ✅ | [backend/Services/CartService.cs](backend/Services/CartService.cs), [backend/Controllers/CartController.cs](backend/Controllers/CartController.cs) |
| **PaymentService + CheckoutController** (ProcessCheckout, Strategy-pattern Stripe integration, webhook processing, signature verify, event log idempotency) | ✅ | [backend/Services/Payments/](backend/Services/Payments/), [backend/Controllers/CheckoutController.cs](backend/Controllers/CheckoutController.cs) |
| **OrderService + OrdersController** (GetOrders, GetOrderById with user ownership check) | ✅ | [backend/Services/OrderService.cs](backend/Services/OrderService.cs), [backend/Controllers/OrdersController.cs](backend/Controllers/OrdersController.cs) |
| Frontend: products list page (paginated, debounced search, sort dropdown, category chip filter, loading skeletons) | ✅ | [frontend/src/app/products/page.tsx](frontend/src/app/products/page.tsx) |
| Frontend: product detail page (dynamic metadata attribute table, stock badges, tags, add-to-cart) | ✅ | [frontend/src/app/products/[id]/page.tsx](frontend/src/app/products/[id]/page.tsx) |
| Frontend: CartContext + useCart (loads on auth, addItem/updateItem/removeItem) | ✅ | [frontend/src/contexts/CartContext.tsx](frontend/src/contexts/CartContext.tsx) |
| Frontend: full cart page (quantity stepper, remove, low stock warning, order summary, Stripe redirect trigger) | ✅ | [frontend/src/app/cart/page.tsx](frontend/src/app/cart/page.tsx) |
| **Frontend: checkout pages** (success transaction detail page, payment cancel redirect page) | ✅ | [frontend/src/app/checkout/](frontend/src/app/checkout/) |
| **Frontend: Order History page** (expandable cards with lazy-loaded item receipts, status badges, frozen pricing) | ✅ | [frontend/src/app/orders/page.tsx](frontend/src/app/orders/page.tsx) |
| Frontend: HeaderNav with user menu, live cart badge count | ✅ | [frontend/src/components/HeaderNav.tsx](frontend/src/components/HeaderNav.tsx) |
| **Backend Unit Tests** (41 tests using xUnit & InMemory DB for Auth, Products, Cart, Orders, and Payments) | ✅ | [backend.Tests/](backend.Tests/) |
| **Frontend Unit Tests** (12 tests using Vitest for helper formatting/parsing logic) | ✅ | [frontend/src/types/product.test.ts](frontend/src/types/product.test.ts) |
| **Containerized Test Configurations** (`Dockerfile.backend.test`, `Dockerfile.frontend.test`, `vitest.config.ts`) | ✅ | Root & [frontend/vitest.config.ts](frontend/vitest.config.ts) |
| Swagger with Bearer auth button | ✅ | `/swagger` |
| Docker: full stack runs, no host tooling, no port clashes | ✅ | [docker-compose.yml](docker-compose.yml) + local `docker-compose.override.yml` |
| Frontend design system implemented (tokens, Tailwind, Inter, base components, reskinned pages) | ✅ | [globals.css](frontend/src/app/globals.css), [tailwind.config.ts](frontend/tailwind.config.ts), [components/](frontend/src/components/) |
| Light/dark theme following the OS (`prefers-color-scheme`) | ✅ | `globals.css` token blocks + `themeColor` in [layout.tsx](frontend/src/app/layout.tsx) |

**Verified behaviour:**
- Auth: register -> auto-login -> JWT issued; login -> JWT; JWT validated on route change; middleware guards `/cart`, `/orders`.
- Products: Search (`q`), Category Chips filter, Sort by newest/price/name. Detail page renders specifications table.
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
- Controllers: thin. Parse request, call a service, translate `Exception → HTTP`. No business logic, no EF queries.
- Services: all business logic + EF access. Interface + impl (e.g. `IAuthService`/`AuthService`), registered in DI.
- DTOs live in `backend/Models/Dtos/**`. **Never** return EF entities from controllers.

**Error handling pattern (established):** throw `AppException`(message, statusCode) or its static factories (`AppException.NotFound()`, `.Conflict()`, `.Forbidden()`) and have the controller map it via `Problem(detail, statusCode)`.

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

## 5. P1 implementation outline (feature by feature)

Legend: 🟢 done · 🟡 partial · 🔴 not started.

### Feature 1 — User Registration & Login — 🟢 DONE
- Login/register pages using the design system.
- `useAuth` hook + `AuthContext`: stores token in localStorage, exposes `user`, `login()`, `register()`, `logout()`. Re-hydrates via `/auth/me`.
- Next.js middleware route guard (`middleware.ts`) protects pages `/cart`, `/checkout`, `/orders`.
- Header user menu shows dropdown when logged in.

### Feature 2 — Product Browsing — 🟢 DONE
- 12 seed products across **5 categories and multiple product types**, with dynamic specifications table from metadata jsonb.
- `ProductService` paginated list with keyword ILike search, category filter, 4 sort modes.
- ProductCard quick add-to-cart button.

### Feature 3 — Shopping Cart — 🟢 DONE
- `Cart` + `CartItem` entities. CartService implements crud with stock boundaries.
- `CartContext` + `useCart` hook. Cart page with stepper, remove, subtotal recalculations.

### Feature 4 — Checkout & Stripe Payment — 🟢 DONE
- `PaymentMethod`, `Payment`, `PaymentWebhook` entities.
- Strategy pattern for payment gateway: `IPaymentStrategy` -> `StripePaymentStrategy`.
- `PaymentService.ProcessCheckoutAsync` creates pending orders and logs pending transactions.
- Stripe webhook processor `PaymentService.HandleWebhookAsync` verifies signatures and logs webhook payloads inside a DB transaction. Prevents duplicate event triggers using unique index. Deducts stock and clears user cart upon success.
- Success and cancel frontend routes displaying references and card links.

### Feature 5 — Order History — 🟢 DONE
- `OrderService` fetches paginated order history and detailed items with owner checks.
- Order history page lists chronological orders with status badges (`Paid`, `Pending`, `Cancelled`).
- Expansion card lazy-loads receipts and shows snapshotted frozen prices at purchase time.

---

## 6. P14-specific requirements beyond the 5 MVP features (priority-tagged)

The P14 spec ([P14_Modern_Ecommerce_Web_App.md](P14_Modern_Ecommerce_Web_App.md)) asks for more than the 5 core MVP features. These are mapped to priority tiers.

| P14 requirement | Priority | Status | Notes / where it lands |
|---|---|---|---|
| **Product type-specific attributes** | **P1** | ✅ DONE | Implemented: dynamic Specifications table from `Product.Metadata` (jsonb). |
| **RBAC** — 3 roles with access control | P2 | 🟡 | Roles seeded + claims in JWT. Missing: `[Authorize(Roles=...)]` on admin endpoints + a 403 path. |
| **Customer profile management** | P2 | 🔴 | `GET/PUT /api/users/me` for profile edit. |
| **Wishlist** | P2 | 🔴 | `Wishlist` entity (user_id, product_id, added_at) + UI toggle. |
| **Guest cart + merge on login** | P2 (basic) / P3 (Redis) | 🟡 | Schema supports this (Cart has nullable `user_id` + `session_id`). Needs merge logic in CartService. |
| **Sorting** (with search/filter) | **P1** | ✅ DONE | Implemented: sorting by newest, price, name. |
| **Dynamic pricing / pricing rules** | P2 (model) / P3 (engine) | 🟡 | Price seam established (`ResolvePrice`). P1 is pass-through. P2 replaces body with rule-based logic. |
| **Email order confirmation** | P2 | 🔴 | Background service + SMTP. |
| **Shipping info + delivery status** | P2 | 🔴 | Address capture + admin delivery updates. |
| **Order status workflow** | P2 | 🟡 | `Order.CurrentStatus` is in place. Missing admin transition controller. |
| **Admin dashboard** | P2 | 🔴 | Product/Inventory/Order CRUD panel. |
| **Analytics dashboard** | P2 | 🔴 | ECharts sales dashboard. |
| **PWA** | P2 | 🟡 | manifest.webmanifest + icons done. Service Worker missing. |

---

## 7. Flat TODO checklist

**Foundation / cross-cutting**
- [x] Introduce a shared `AppException`(message, statusCode) + a controller/exception-handling helper (generalise the `AuthException` pattern).
- [x] Add global validation error shaping (consistent 400 body).
- [x] Frontend: design tokens (globals.css light + dark via `prefers-color-scheme`) + tailwind mapping + Inter font + base components (Button, Card, Badge, ProductCard, EmptyState).
- [x] Frontend: add an `Input` component (needed for auth/search forms) to complete the base set.
- [x] Frontend: `useAuth` + `useCart` contexts/hooks; wire `apiCall` token injection.

**Frontend shell polish**
- [x] Favicon + maskable icon + PWA web manifest.
- [x] Styled `not-found.tsx` (404), route-level `loading.tsx`, and `error.tsx`.
- [x] `ProductCard` links to `products/[id]`; product detail route added.
- [x] Wire the "Add to cart" button to `useCart`.

**Feature 1 — Auth (DONE ✅)**
- [x] Login page, Register page (design system).
- [x] Auth context + persisted token + header user menu + logout.
- [x] Next.js middleware route guard for protected routes.
- [x] `POST /api/auth/logout` endpoint.

**Feature 2 — Products (DONE ✅)**
- [x] Seed real products + categories across multiple product types with populated metadata.
- [x] `ProductService` + DTOs; DB-backed list and detail.
- [x] Search + category filter + sorting.
- [x] `GetEffectivePrice()` / `ResolvePrice()` price seam.
- [x] Product list page + ProductCard.
- [x] Product detail page with dynamic metadata table.

**Feature 3 — Cart (DONE ✅)**
- [x] `Cart` + `CartItem` entities + migration.
- [x] `CartService` + 4 cart endpoints.
- [x] Cart page with quantity stepper, removal, subtotal.

**Feature 4 — Checkout & Stripe (DONE ✅)**
- [x] `Payment` + `PaymentMethod` + `PaymentWebhook` entities + migration; seed `stripe`.
- [x] `POST /api/checkout` (create order + Stripe session), Strategy-pattern provider.
- [x] `POST /api/checkout/webhook/stripe` (idempotent, signature-verified, order→paid, clear cart, deduct stock).
- [x] Success and cancel checkout landing pages.

**Feature 5 — Orders (DONE ✅)**
- [x] `OrderService`; `GET /api/orders`, `GET /api/orders/{id}` (ownership validated).
- [x] Expandable Orders list displaying snapshotted frozen prices.

**Polish / P1 exit criteria**
- [x] End-to-end happy path: register → browse → add to cart → checkout (test card) → see order in history.
- [x] All pages responsive (4→3→2→1 grid, rem units) and on the design system.
- [x] Basic tests: xUnit service tests (auth, cart, products, orders, payments) + Vitest helper tests (formatters, parsers) fully containerized in Docker.
