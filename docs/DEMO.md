# Novacart — Demo Guide

> A scripted walkthrough for presenting Novacart. Covers the end-to-end happy path plus the
> admin/analytics/PWA highlights. Approximate duration: 10–12 minutes.

---

## Before you start

1. **Bring the stack up** (clean state recommended):
   ```bash
   docker compose down -v        # wipe old data
   docker compose up --build -d  # rebuild + start
   ```
2. Wait for the backend to migrate + seed (watch `docker compose logs -f backend` until you see startup complete).
3. Open the storefront: **http://localhost:3001**.
4. Open a second tab for Swagger: **http://localhost:5000/swagger** (useful to show the API surface).
5. Have these credentials ready:

| Role | Email | Password |
|---|---|---|
| Admin / SysAdmin (dev) | `admin@novacart.local` | `Admin123!` |
| Customer (register fresh) | *(create during demo)* | *(choose)* |

6. Stripe test card: **`4242 4242 4242 4242`**, any future expiry / CVC / postcode.

---

## Demo script

### Act 1 — Customer journey (5 min)

1. **Register a new customer.** Show the auto-login and that the cart badge / user menu appear. Mention the **HttpOnly cookie** (JWT never touches JS → XSS-safe).

2. **Browse the catalogue.**
   - Show the responsive grid (resize the window: 4 → 3 → 2 → 1 columns).
   - Demonstrate **faceted search**: type a keyword, tick multiple categories, set a price range, pick a tag chip, change sort.
   - Open a **product detail** — point out the *dynamic specifications table* (metadata-driven, different attributes per product type).

3. **Add to cart + wishlist.**
   - Add 2–3 products; show the live cart badge.
   - Heart a product → show it on the Wishlist page.

4. **Checkout with Stripe.**
   - Go to cart → **Proceed to checkout** → pick a shipping address (add one under Account if needed).
   - Redirected to Stripe sandbox → pay with `4242…`. Emphasise **no card data touches our server** (tokenisation).
   - Land on the **success page**.

5. **Order history.** Show the paid order, expand it — highlight the **frozen prices** (snapshot at purchase).

6. **PWA (optional).** Show the install icon in the address bar; install; demonstrate offline (DevTools → Network → Offline → reload → offline page).

> **Talking points:** dynamic pricing, Redis caching (fast product/order reads), idempotent webhook (safe to retry).

### Act 2 — Admin capabilities (4 min)

Log in as `admin@novacart.local`.

1. **Admin dashboard** — show the sidebar nav (collapses to hamburger below 768px).

2. **Product management** (`/admin/products`):
   - Filter by status, search, paginate.
   - **Edit a product**: change its price or stock; show the low-stock badge.
   - **Create a pricing rule** → go to `/admin/pricing`, add a 20%-off rule on a category → return to the storefront and show the updated price + strikethrough compare-at price.

3. **Order management** (`/admin/orders`):
   - Find the order from Act 1.
   - **Advance its status**: paid → processing → shipped. Note each step sends the customer an email and writes an audit-history row.

4. **Analytics** (`/admin/analytics`):
   - Show the KPI cards (revenue, orders, AOV).
   - The **sales-over-time** ECharts chart (follows the theme — toggle OS dark mode to show it adapt).

> **Talking points:** RBAC (3 roles), the validated order state machine, ECharts theming with design tokens.

### Act 3 — System admin + engineering depth (2 min)

1. **System diagnostics** (`/admin/system`) — only visible to `sysadmin`. Show the DB/Redis health probes and the **clear-cache** action.

2. **Swagger** (`/swagger`) — show the API surface, the Bearer-auth button, and that every endpoint documents its responses (`[ProducesResponseType]`).

3. **(Optional) Run the tests live:**
   ```bash
   docker build -f Dockerfile.backend.test -t novacart-backend-test . && docker run --rm novacart-backend-test
   docker build -f Dockerfile.frontend.test -t novacart-frontend-test . && docker run --rm novacart-frontend-test
   ```

> **Talking points:** layered architecture (Controller → Service → Mapper → Entity), Strategy (payments) + Factory (orders), CI/CD pipeline, global exception handling.

---

## Screenshot checklist

Capture these for a static portfolio / README:

- [ ] Storefront homepage (hero + backend status card)
- [ ] Products page with active filters (category chips + price + search)
- [ ] Product detail with dynamic specs table
- [ ] Cart page (stepper + order summary + low-stock warning)
- [ ] Stripe Checkout redirect
- [ ] Order history (expanded card with frozen prices)
- [ ] Admin product table (status badges, pagination)
- [ ] Admin pricing rule form + compare-at price on storefront
- [ ] Admin order detail + status workflow buttons
- [ ] Analytics dashboard (KPIs + ECharts chart, light + dark)
- [ ] System diagnostics (sysadmin view)
- [ ] PWA install prompt / standalone window
- [ ] Swagger UI

---

## Test data reference

- **Seed catalogue**: 12 products across 5 categories (Electronics, Apparel, Home & Living, Accessories, Books), each with `metadata` + Unsplash images.
- **Seed roles**: `customer` (1), `admin` (2), `sysadmin` (3).
- **Dev admin**: `admin@novacart.local` / `Admin123!` (auto-seeded in Development).
- **Stripe test card**: `4242 4242 4242 4242`.

---

## Related

- [User Guide](USER-GUIDE.md) · [Architecture](ARCHITECTURE.md) · [UI Design](UI-DESIGN.md)
