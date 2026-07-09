# Novacart — P1 (MVP) Handoff & TODO

> **Purpose of this file.** A self-contained handoff so another AI/developer can continue building
> the Priority-1 MVP without re-discovering context. It records what is already done, how to run the
> project, the conventions to follow, the frontend design system to build against, and a
> feature-by-feature implementation plan with a flat checklist.
>
> Last updated: 2026-07-09. Scope: **Priority 1 — MVP Core Features** only (see [README.md](README.md) §Priority 1).

---

## 0. TL;DR for whoever picks this up

- Backend: ASP.NET Core 8 + EF Core + PostgreSQL + Redis. Frontend: Next.js 14 (App Router) + TS + Tailwind.
- **Auth (register / login / JWT) is DONE and verified.** Entities + first migration are DONE.
- **Everything runs in Docker** — one command, no host .NET/Node needed: `docker compose up --build -d`.
- The DB **auto-migrates on startup** in Development, so a fresh `docker compose up` gives you tables + seeded roles.
- Next up, in order: **Product browsing → Cart → Checkout/Stripe → Order history** (details in §5).
- Follow the **layered architecture** and the **design system** in this doc; don't invent new patterns.

---

## 1. Current status — what is DONE ✅

| Area | Status | Where |
|---|---|---|
| Entity model (User, Role, UserRole, Category, Product, Order, OrderItem) | ✅ | [backend/Models/Entities/](backend/Models/Entities/) |
| EF DbContext: keys, unique indexes, decimal precision, relationships | ✅ | [backend/Data/AppDbContext.cs](backend/Data/AppDbContext.cs) |
| Seed data: roles `customer` / `admin` / `sysadmin` | ✅ | `AppDbContext.OnModelCreating` (HasData) |
| Initial migration `InitialCreate` (8 tables) | ✅ | [backend/Data/Migrations/](backend/Data/Migrations/) |
| Auto-migrate on startup (Development) | ✅ | [backend/Program.cs](backend/Program.cs) |
| Password hashing (bcrypt) | ✅ | `AuthService` (BCrypt.Net-Next) |
| JWT issuing (HS256, sub/email/name/role claims) | ✅ | [backend/Services/JwtTokenService.cs](backend/Services/JwtTokenService.cs) |
| Auth endpoints: `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me` | ✅ | [backend/Controllers/AuthController.cs](backend/Controllers/AuthController.cs) |
| JWT bearer auth + authorization middleware wired | ✅ | [backend/Program.cs](backend/Program.cs) |
| Swagger with Bearer auth button | ✅ | `/swagger` |
| Docker: full stack runs, no host tooling, no port clashes | ✅ | [docker-compose.yml](docker-compose.yml) + local `docker-compose.override.yml` |
| Frontend design system implemented (tokens, Tailwind, Inter, base components, reskinned pages) | ✅ | [globals.css](frontend/src/app/globals.css), [tailwind.config.ts](frontend/tailwind.config.ts), [components/](frontend/src/components/) |
| Light/dark theme following the OS (`prefers-color-scheme`) | ✅ | `globals.css` token blocks + `themeColor` in [layout.tsx](frontend/src/app/layout.tsx) |

**Verified behaviour** (curl, in Docker): register → 200 + JWT + auto-assigned `customer` role;
login → 200 + JWT; `/me` with token → 200; duplicate email → 409; wrong password → 401;
short password → 400; `/me` without token → 401.

### Entities that still need to be created for P1
The ER design ([Database_ER_Diagram.md](Database_ER_Diagram.md)) has more tables than we've built. For the P1 MVP you still need:
- **`Cart`, `CartItem`** — for the shopping cart (Feature 3).
- **`Payment`, `PaymentMethod`** — for checkout/Stripe (Feature 4). Seed one `PaymentMethod` row: `stripe`.
- *(Optional for P1)* `UserAddress` — if you collect a shipping address at checkout. You can start by storing address as JSON on the order (`Order` already anticipates `jsonb` shipping/billing in the ER) or add the table.

Order status history / transitions tables are **P2 (admin)** — skip for P1.

---

## 2. How to run (Docker-only, this machine)

```bash
cd "Novacart"
docker compose up --build -d        # builds net8 backend + Next.js frontend, starts everything
docker compose logs -f backend      # watch startup / migration logs
docker compose down                 # stop
docker compose down -v              # stop + wipe DB/redis volumes
```

| Service | URL |
|---|---|
| Frontend | http://localhost:**3001** (remapped locally; see note) |
| Backend API | http://localhost:5000 |
| Swagger | http://localhost:5000/swagger |
| Health | http://localhost:5000/api/health |

**Smoke test:**
```bash
curl -X POST http://localhost:5000/api/auth/register -H "Content-Type: application/json" \
  -d '{"email":"you@example.com","password":"Sup3rSecret!","fullName":"Your Name"}'
```

### ⚠️ This machine's specifics (already handled — don't re-solve)
- Host has **only .NET SDK 10** (no net8 runtime) and **another docker stack (`marketing-simplified`) permanently binds host ports 5432 / 6379 / 3000**.
- **Docker is the supported path** and sidesteps both problems: the backend image is `mcr.microsoft.com/dotnet/{sdk,aspnet}:8.0`, and a local, git-ignored **`docker-compose.override.yml`** stops publishing Postgres/Redis to the host (they talk over the internal network) and moves the frontend to **3001**.
- If you ever must run the backend on the **host** (not recommended here): `export DOTNET_ROLL_FORWARD=Major` and point the connection string at a Postgres you control (e.g. a container on port 5433). Prefer Docker.

---

## 3. Architecture & conventions (follow these)

**Layering:** `Controller → Service → Entity` (add a `Mapper`/DTO-projection step as things grow).
- Controllers: thin. Parse request, call a service, translate `Exception → HTTP`. No business logic, no EF queries.
- Services: all business logic + EF access. Interface + impl (e.g. `IAuthService`/`AuthService`), registered in DI.
- DTOs live in `backend/Models/Dtos/**`. **Never** return EF entities from controllers (avoid over-posting / cycles).
- Entities live in `backend/Models/Entities/`.

**Error handling pattern (established):** throw a typed exception carrying an HTTP status
(see `AuthException`), and have the controller map it via `Problem(detail, statusCode)`.
Reuse this pattern for other features (e.g. a shared `AppException`), rather than returning ad-hoc objects.

**EF / DB conventions (established):**
- Table names: snake_case via `ToTable("...")`. Guid PKs for User/Product/Order; int PKs for Role/Category.
- Money: `decimal` with `HasPrecision(18, 2)`. Currency default `"AUD"`.
- Unique indexes named `idx_<table>_<col>`; other indexes likewise. Follow the ER doc's index plan.
- Postgres-native types where the ER calls for them: `text[]` (Product.Tags), `jsonb` (Product.Metadata).

**Add a migration** (from host with the roll-forward note, or `docker compose exec`):
```bash
export DOTNET_ROLL_FORWARD=Major        # host only; not needed inside the net8 container
dotnet ef migrations add <Name> -o Data/Migrations
# no need to `database update` manually — the app auto-migrates on startup in Development
```

**Auth model decision (current):** JWT returned in the response **body**; frontend stores it and sends
`Authorization: Bearer <token>` (matches [frontend/src/lib/api.ts](frontend/src/lib/api.ts)).
The README also mentions HttpOnly-cookie storage (more XSS-resistant). **Decision point:** keep Bearer for
MVP simplicity, or switch to HttpOnly cookies + CSRF handling. If you switch, update `api.ts` and CORS
(`AllowCredentials`) accordingly. Don't do both half-way.

---

## 4. Frontend design system — the "基调" (build against this)

**Positioning:** Per the P14 spec ([P14_Modern_Ecommerce_Web_App.md](P14_Modern_Ecommerce_Web_App.md)), Novacart is a
**general-purpose, multi-category e-commerce platform** — it must sell *any* product type (electronics, apparel,
home goods, …) and label each with **type-specific attributes** ("products labelled with matching details based
on product type"). The seed items (ceramic mug, tote, …) are just placeholder data, **not** a brand vertical.

So the tone is deliberately **neutral, content-first, and adaptable**: a clean, trustworthy retail UI where the
**product imagery carries the color** and the chrome stays quiet. Think modern marketplace (Shopify/SSENSE/Everlane
neutrality), not a niche boutique. This replaces the placeholder `indigo-600` styling.

> **Status: this system is now IMPLEMENTED in code** — tokens in [globals.css](frontend/src/app/globals.css) (light + dark
> via `prefers-color-scheme`, following the OS), mapped in [tailwind.config.ts](frontend/tailwind.config.ts), Inter wired via
> `next/font` in [layout.tsx](frontend/src/app/layout.tsx), base components ([Button](frontend/src/components/ui/Button.tsx),
> [Card](frontend/src/components/ui/Card.tsx), [Badge](frontend/src/components/ui/Badge.tsx),
> [ProductCard](frontend/src/components/ProductCard.tsx)), and all four pages reskinned off `indigo-*`. The spec below is the
> reference the code follows — keep them in sync. **Not yet built:** functional wiring (auth pages, cart/`useCart`, real product
> detail with the metadata attribute table) — those come with the P1 features in §5.

**Design principles**
1. **Neutral canvas, product does the talking.** White/near-white surfaces, restrained gray UI; photos provide the color.
2. **One themeable accent.** A single brand/action color drives buttons, links, active states — swap one token to rebrand.
3. **Category-agnostic & type-aware.** Layouts must render heterogeneous products; product detail shows a **dynamic attribute table** from `products.metadata` (jsonb).
4. **Clarity & trust.** Legible type, obvious prices, clear stock/sale states, strong focus states — this is a store, not a showcase.
5. **Mobile-first & rem-based** (P14 requires rem units, PWA, Grid breakpoints 4→3→2→1).
6. **Everything is a token.** All colors/space/radii via CSS custom properties so the platform can be themed per deployment.

### Color tokens (light)
Neutral grays + **one swappable accent**. Default accent is a conventional, trustworthy blue — change `--accent` alone to rebrand.

| Token | Hex | Use |
|---|---|---|
| `--bg` | `#FFFFFF` | Page background |
| `--bg-subtle` | `#F5F6F8` | Alternating sections, filter rails, wells |
| `--surface` | `#FFFFFF` | Cards, sheets, header |
| `--border` | `#E5E7EB` | Hairline borders/dividers |
| `--ink` | `#111827` | Primary text (neutral gray-900) |
| `--ink-muted` | `#6B7280` | Secondary text, captions, meta |
| `--accent` (brand) | `#2563EB` | Primary actions, links, active/selected — **the one token to rebrand** |
| `--accent-hover` | `#1D4ED8` | Hover/pressed for primary |
| `--accent-weak` | `#EFF4FF` | Accent-tinted backgrounds (selected chips, info banners) |
| `--success` | `#16A34A` | In stock, confirmations |
| `--warning` | `#D97706` | Low stock, caution |
| `--danger` | `#DC2626` | Errors, destructive, out of stock |
| `--sale` | `#DC2626` | Discounted price / sale badge |
| `--rating` | `#F59E0B` | Star ratings |
| `--focus-ring` | `--accent` @ 45% | Focus outline (2px) |

Neutral scale (for shadows/fills): `#F9FAFB #F3F4F6 #E5E7EB #D1D5DB #9CA3AF #6B7280 #374151 #111827`.

**Dark mode** (P14 PWA benefits from it; do it via `prefers-color-scheme` + a `data-theme` override):
`--bg #0B0E14`, `--bg-subtle #131722`, `--surface #131722`, `--border #262B36`, `--ink #E5E7EB`,
`--ink-muted #9CA3AF`, lighten `--accent` to `#3B82F6`. Keep semantic hues, nudge lighter for contrast.

### Typography
Neutral and workhorse — **one family**, no editorial serif (that would bias toward a vertical).
- **UI + headings + body:** `Inter` (variable; highly legible at all sizes). Weights 400 / 500 / 600 / 700.
  Headings = Inter 600–700 with tight tracking (`-0.01em`), *not* a different family.
- Load via `next/font/google` (self-hosted, no CDN). Fallback: `system-ui, -apple-system, Segoe UI, Roboto, sans-serif`.
- **Type scale (rem):** 0.75 / 0.8125 / 0.875 / 1 / 1.125 / 1.25 / 1.5 / 1.875 / 2.25 / 3. Body 1rem/1.5; headings 1.2.
- Tabular numerals for prices/quantities (`font-variant-numeric: tabular-nums`).

### Shape, space, elevation
- **Spacing:** 4px base; rem tokens (0.25/0.5/0.75/1/1.5/2/3/4rem). Container `max-width: 80rem` (1280px), centered, responsive `padding-inline`.
- **Radius:** cards `8px`, buttons/inputs `8px`, chips/pills `9999px`, images `8px`. Moderate — neither sharp nor bubbly.
- **Elevation:** neutral, subtle. Card rest: `0 1px 2px rgba(17,24,39,.06)`; hover: `0 4px 16px rgba(17,24,39,.10)` + `translateY(-2px)`. Prefer `--border` for structure; shadows only on lift/overlays.

### Components (baseline specs)
- **Button (primary):** bg `--accent`, text white, radius 8px, padding `0.625rem 1rem`, weight 600; hover `--accent-hover`; 2px `--focus-ring`; disabled 40% opacity.
- **Button (secondary):** `--surface` bg, `1px solid --border`, text `--ink`; hover bg `--bg-subtle`.
- **Button (ghost/quiet):** transparent, text `--ink-muted`→`--ink` on hover (icon buttons, table actions).
- **Product card:** `--surface`, 1px `--border`, 8px radius; **1:1 or 4:5** image on a `--bg-subtle` placeholder; category/brand in `--ink-muted` (small caps optional); name in Inter 600 ~1rem (2-line clamp); price bold `--ink` with optional `--sale` strikethrough of the compare-at price; stock/`New`/`Sale` badge; quick "Add to cart"; hover = lift. **Must render cleanly for any category** — no vertical-specific ornament.
- **Product detail:** gallery + info; **dynamic attribute table** driven by `metadata` (e.g. apparel → size/material; electronics → RAM/CPU); quantity stepper; add-to-cart + add-to-wishlist (heart); stock state; price + sale.
- **Filter/sort rail:** left sidebar (collapses to a sheet on mobile); faceted filters (category, price range, tags/attributes), sort dropdown; selected facets as removable `--accent-weak` chips.
- **Input / select:** `--surface`, 1px `--border`, radius 8px, `--accent` focus ring, label above, helper/error text below (`--danger`).
- **Header/nav:** slim, sticky, `--surface` + bottom hairline; wordmark (plain Inter 700); search bar center; account + wishlist + cart icons right, cart/wishlist counts as `--accent`/`--danger` badges.
- **Badges:** neutral (`--bg-subtle`/`--ink-muted`) by default; semantic fills for `In stock`/`Low stock`/`Out of stock`/`Sale`.
- **Empty / loading / error states:** every list (products, cart, wishlist, orders) needs all three; use skeletons (neutral shimmer), not spinners, for grids.

### Motion, icons, imagery, voice
- **Motion:** 150–200ms `ease-out`; hover lifts, fades, count-badge bumps, sheet/drawer slides. Respect `prefers-reduced-motion`.
- **Icons:** `lucide-react`, 1.5–2px stroke, `--ink`/`--ink-muted`.
- **Imagery:** product on neutral/white seamless; enforce a consistent aspect ratio (pick 1:1 **or** 4:5 and keep it); placeholders use `--bg-subtle` blocks. Always set `alt` + `width/height` (CLS).
- **Voice:** clear, neutral, trustworthy — plain retail language ("Add to cart", "Save for later", "Your cart is empty"). No cutesy niche copy.

### Implementation notes
- Put tokens as CSS custom properties in [frontend/src/app/globals.css](frontend/src/app/globals.css) `:root` (+ dark overrides) and map them in
  [frontend/tailwind.config.ts](frontend/tailwind.config.ts) `theme.extend.colors` (`bg`, `bg-subtle`, `surface`, `border`, `ink`, `ink-muted`, `accent`, …)
  so you can write `bg-accent text-surface border-border`. **Rebranding = change `--accent` (+ hover/weak).**
- Add the Inter font via `next/font/google` in [frontend/src/app/layout.tsx](frontend/src/app/layout.tsx); expose as CSS var `--font-sans`.
- **Migrate the existing stub pages off `indigo-*`** to these tokens as you touch them.
- Accessibility (P14 lists HCI): WCAG AA contrast (accent-on-white and ink-on-white both pass); visible focus ring on every interactive element; keyboard-operable filters/menus; all images have `alt`.
- PWA: this system is the base for the installable app — keep the header/nav usable in standalone mode and test the theme-color against `--surface`.

---

## 5. P1 implementation outline (feature by feature)

Legend: 🟢 done · 🟡 partial · 🔴 not started.

### Feature 1 — User Registration & Login — 🟡 (backend done, frontend pending)
**Done:** entities, bcrypt, JWT, `register`/`login`/`me`, middleware, Swagger.
**Remaining (frontend):**
- `(auth)/login` and `(auth)/register` pages using the design system.
- A `useAuth` hook + auth context: store token, expose `user`, `login()`, `register()`, `logout()`.
- Route guard (Next.js middleware) → redirect unauthenticated users away from protected routes.
- `POST /api/auth/logout` endpoint (Bearer model: client-side token clear is enough; add server endpoint for symmetry / future cookie model).
**Acceptance:** a new visitor can register, is logged in, sees their name in the header, and stays logged in across refresh; protected pages redirect when logged out.

### Feature 2 — Product Browsing — 🔴 (backend hardcoded, needs DB)
Current [ProductsController.cs](backend/Controllers/ProductsController.cs) returns **hardcoded** products.
**Backend:**
- Seed real products across **several categories AND product types** (e.g. apparel, electronics, home) with populated `Metadata` (jsonb) — this proves the "general marketplace / type-specific details" requirement, not a single vertical. (Extend `HasData` or a seeding service.)
- `ProductService` + DTOs; rewrite `GET /api/products` (paginated) and `GET /api/products/{id}` to read the DB. Include `metadata` + `tags` in the detail DTO.
- `GET /api/products/search?q=&category=&sort=` — keyword (ILIKE / full-text) + category filter + basic sort.
- Route price reads through a `GetEffectivePrice()` seam (P1 = return `product.Price`) so P2 **dynamic pricing** slots in without refactors (see §6).
- *(Later)* swap the seed source for the **Square Catalogue API** (sandbox) behind the same service interface.
**Frontend:**
- `products/` list: responsive Grid 4→3→2→1, `ProductCard`, loading/empty states, search box + category filter + sort.
- `products/[id]` detail page: gallery, description, price, add-to-cart, and a **data-driven attribute table rendered from `metadata`** (never hardcode per-type fields).
- Use the unified `apiCall` wrapper in [frontend/src/lib/api.ts](frontend/src/lib/api.ts).
**Acceptance:** products come from Postgres; search/filter/sort work; detail page renders type-specific attributes from `metadata`; grid is responsive.

### Feature 3 — Shopping Cart — 🔴
**Backend:** create `Cart` + `CartItem` entities & migration. `CartService`.
- `GET /api/cart`, `POST /api/cart/items`, `PUT /api/cart/items/{id}`, `DELETE /api/cart/items/{id}`.
- Cart keyed by `user_id` for logged-in users; persist across sessions. (Guest cart via `session_id` is optional for P1.)
- *(Enhancement)* Redis-backed cart for speed — optional; DB is fine for MVP.
**Frontend:** cart page (rebuild the stub [cart/page.tsx](frontend/src/app/cart/page.tsx)), `CartItem` + `CartSummary` components,
`useCart` hook + context, real-time totals, quantity update/remove, cart-count badge in header.
**Acceptance:** add/update/remove works, totals recalc live, cart survives logout/login and refresh.

### Feature 4 — Checkout & Payment (Stripe sandbox) — 🔴
**Backend:** create `Payment` + `PaymentMethod` entities (seed `stripe`) & migration.
- `POST /api/checkout` → create an `Order` (status `pending`) from the cart + a Stripe Checkout Session / PaymentIntent; return client secret / redirect URL.
- `POST /api/webhooks/stripe` → verify signature, **idempotently** record the event, mark order `paid` on success, clear the cart.
- Use the **Strategy pattern** for payment providers (README §13) so PayPal/etc. can be added later.
- Secrets from config/env (`Stripe:SecretKey`, `Stripe:WebhookSecret`).
**Frontend:** checkout page — order summary, Stripe Elements/Checkout redirect, success & cancel states.
**Local webhooks:** `stripe listen --forward-to localhost:5000/api/webhooks/stripe` (or ngrok). No card data stored server-side.
**Acceptance:** a test-card payment moves the order `pending → paid` via webhook, cart clears, no real charge.

### Feature 5 — Order History — 🔴 (entities exist)
`Order` / `OrderItem` entities already exist.
**Backend:** `OrderService`; `GET /api/orders` (current user, paginated) and `GET /api/orders/{id}`.
Snapshot product name + price into `OrderItem` at purchase (fields already present). *(Optional)* Redis cache recent orders.
**Frontend:** rebuild the stub [orders/page.tsx](frontend/src/app/orders/page.tsx) — list with date, order #, total, status; detail view with line items.
**Acceptance:** a logged-in user sees their past orders with correct frozen prices and status; other users' orders are never visible.

---

## 6. P14-specific requirements beyond the 5 MVP features (priority-tagged)

The P14 spec ([P14_Modern_Ecommerce_Web_App.md](P14_Modern_Ecommerce_Web_App.md)) asks for more than the 5 core MVP
features above. These are mapped to the README's priority tiers (P1 = MVP · P2 = P14 requirements · P3 = enhancements).
**Don't build these during P1** unless flagged P1 — but know they're coming so the P1 data model doesn't block them
(the [ER schema](Database_ER_Diagram.md) already anticipates most).

| P14 requirement | Priority | Status | Notes / where it lands |
|---|---|---|---|
| **Product type-specific attributes** (label products with matching details per type) | **P1** | 🔴 | Core to a *general* store. `Product.Metadata` (jsonb) already exists — seed products of **varied types** and render a dynamic attribute table on the detail page. Folded into Feature 2. |
| **RBAC** — 3 roles with access control | P2 | 🟡 | Roles seeded + role claims already in the JWT. Missing: `[Authorize(Roles=...)]` on admin endpoints + a 403 path. |
| **Customer profile management** | P2 | 🔴 | `GET/PUT /api/users/me`; edit name/email/password. |
| **Wishlist** | P2 | 🔴 | New `Wishlist` entity (user_id, product_id, added_at) + endpoints; heart toggle on card/detail; wishlist page. |
| **Guest cart + merge on login** | P2 (basic) / P3 (Redis) | 🔴 | ER `carts` already has nullable `user_id` + `session_id`. Store guest cart (local + server by session), then **merge into the user cart on login**. Redis-backed cross-device is the P3 optimisation. |
| **Sorting** (with search/filter) | P2 | 🔴 | Part of README §9 advanced search; a basic price/newest sort can ride along with Feature 2's search. |
| **Dynamic pricing / configurable pricing rules** (+ manual) | P2 (model + admin UI) / P3 (rule engine) | 🔴 | Distinctive P14 feature. Needs a `PricingRule`/price-resolution service applied at add-to-cart & checkout, and "view purchase history **with the dynamic pricing** applied". `OrderItem.PriceAtPurchase` already freezes the resolved price. |
| **Email / message order confirmation** | P2 | 🔴 | Background service + SMTP (dev: Mailhog/console). README's RabbitMQ pipeline is the P3 version. |
| **Shipping info + delivery status/updates** | P2 | 🔴 | Capture shipping address (Order jsonb or `UserAddress`); admin updates delivery status; surface to customer. |
| **Order status workflow** (`pending→paid→processing→shipped→completed→cancelled`), admin-updatable | P2 | 🔴 | `Order.CurrentStatus` exists; add transitions + (optionally) the ER's history/transition tables. Admin action. |
| **Admin dashboard** — product/inventory/order management | P2 | 🔴 | README §7. Admin-only area (behind RBAC). |
| **Analytics dashboard** — total sales, orders/day, revenue, best-selling | P2 | 🔴 | README §7; ECharts. |
| **PWA** — manifest, service worker, installable, standalone | P2 | 🔴 | README §6. Build on the design system's neutral base. |
| **Mobile-first responsive** (mobile/tablet/desktop) | P2 (cross-cutting) | 🟡 | Design system (§4) defines it; apply as each page is built. |

**Non-obvious ones worth reading twice:**
- **Guest cart merge** shapes the Cart design *now* — build `Cart` with a nullable `user_id` + `session_id` from the start (even if P1 only uses the logged-in path) so P2 merge is data-compatible, not a rewrite.
- **Dynamic pricing** should route *all* price reads through one resolver (`GetEffectivePrice(product, user, context)`), even if P1's resolver just returns `product.Price`. Retrofitting a pricing layer later is painful; a pass-through seam now is cheap.
- **Type-specific attributes** mean the product detail UI must be **data-driven** (loop over `metadata` keys), never hardcode fields — otherwise the "general marketplace" goal breaks the first time a new product type appears.

---

## 7. Flat TODO checklist

**Foundation / cross-cutting**
- [ ] Introduce a shared `AppException`(message, statusCode) + a controller/exception-handling helper (generalise the `AuthException` pattern).
- [ ] Add global validation error shaping (consistent 400 body).
- [ ] Frontend: set up design tokens (globals.css `:root` + dark overrides + tailwind.config), Inter font, and base UI components (Button, Input, Card).
- [ ] Frontend: `useAuth` + `useCart` contexts/hooks; wire `apiCall` token injection to the auth store.

**Feature 1 — Auth (finish)**
- [ ] Login page, Register page (design system).
- [ ] Auth context + persisted token + header user menu + logout.
- [ ] Next.js middleware route guard for protected routes.
- [ ] `POST /api/auth/logout` endpoint.

**Feature 2 — Products**
- [ ] Seed real products + categories **across multiple product types** with populated `metadata` (jsonb).
- [ ] `ProductService` + DTOs (include `metadata`/`tags`); DB-backed `GET /api/products`, `GET /api/products/{id}`.
- [ ] `GET /api/products/search` (keyword + category + basic sort).
- [ ] `GetEffectivePrice()` price seam (P1: pass-through) for future dynamic pricing.
- [ ] Product list page (responsive grid, search, filter, sort) + `ProductCard`.
- [ ] Product detail page with **data-driven attribute table from `metadata`**.
- [ ] *(Later)* Square Catalogue API integration behind `ProductService`.

**Feature 3 — Cart**
- [ ] `Cart` (nullable `user_id` + `session_id`, ready for guest/merge) + `CartItem` entities + migration.
- [ ] `CartService` + 4 cart endpoints.
- [ ] Cart page, `CartItem`/`CartSummary`, `useCart`, header cart badge.

**Feature 4 — Checkout / Stripe**
- [ ] `Payment` + `PaymentMethod` entities + migration; seed `stripe`.
- [ ] `POST /api/checkout` (create order + Stripe session), Strategy-pattern provider.
- [ ] `POST /api/webhooks/stripe` (idempotent, signature-verified, order→paid, clear cart).
- [ ] Checkout page (Stripe Elements/Checkout) + success/cancel.
- [ ] Document `stripe listen` / ngrok flow in README.

**Feature 5 — Orders**
- [ ] `OrderService`; `GET /api/orders`, `GET /api/orders/{id}` (scoped to current user).
- [ ] Orders list + detail pages.
- [ ] *(Optional)* Redis cache for recent orders.

**Polish / P1 exit criteria**
- [ ] End-to-end happy path: register → browse → add to cart → checkout (test card) → see order in history.
- [ ] All pages responsive (4→3→2→1 grid, rem units) and on the design system (no leftover `indigo-*`).
- [ ] Basic tests: a few xUnit service tests (auth, cart, order) + a couple of Vitest component tests.

**P2 / P3 backlog (P14 requirements — do NOT start during P1; see §6 for details & priorities)**
- [ ] RBAC enforcement: `[Authorize(Roles=…)]` on admin endpoints + 403 handling *(P2)*.
- [ ] Customer profile management (`GET/PUT /api/users/me`) *(P2)*.
- [ ] Wishlist: entity + endpoints + heart UI + wishlist page *(P2)*.
- [ ] Guest cart + merge-on-login *(P2; Redis cross-device = P3)*.
- [ ] Dynamic pricing: `PricingRule` model + resolver wired into the price seam + admin config UI *(P2; complex engine = P3)*.
- [ ] Email/message order confirmation *(P2)*.
- [ ] Shipping address capture + delivery status updates *(P2)*.
- [ ] Order status workflow + admin update UI *(P2)*.
- [ ] Admin dashboard: product/inventory/order management *(P2)*.
- [ ] Analytics dashboard (ECharts): total sales, orders/day, revenue, best-selling *(P2)*.
- [ ] PWA: manifest + service worker + installable + standalone *(P2)*.

---

## 8. Environment variables / secrets needed

Copy [.env.example](.env.example) → `.env` and fill in before Feature 4:
- `Stripe:SecretKey` / `STRIPE_SECRET_KEY` (sk_test_…), `STRIPE_PUBLISHABLE_KEY` (pk_test_…), `STRIPE_WEBHOOK_SECRET` (whsec_…)
- `Square:AccessToken` / `SQUARE_ACCESS_TOKEN` (only when doing the Square integration)
- `Jwt:Secret` — already set to a dev value in appsettings; **override in production** with a ≥32-char secret.
- DB/Redis connection strings are already wired for Docker (`postgres:5432`, `redis:6379`).

Backend reads config via ASP.NET config keys (`Stripe:SecretKey`) or `__`-style env vars
(`Stripe__SecretKey`) — the latter is how docker-compose passes overrides.

---

## 9. Gotchas / notes for the next AI

- **Run in Docker.** Host has net8-vs-net10 mismatch and port conflicts; Docker avoids both (see §2).
- The local **`docker-compose.override.yml` is git-ignored** and machine-specific (port remaps). Don't commit it; don't rely on its exact ports in code — always use service names inside compose.
- **DB auto-migrates on startup in Development.** After adding a migration, just restart the backend container. Don't hand-run `database update` for the dockerised DB.
- Frontend is on **:3001** locally (not 3000) because of the other stack.
- Keep the **Bearer-token** auth model consistent across backend + `api.ts` unless you deliberately switch to HttpOnly cookies (then handle CORS credentials + CSRF).
- Nothing here is committed yet — the entities/auth/docker work is uncommitted on the working tree. Consider committing before large new work.
