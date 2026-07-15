# Novacart — User Guide

> How to use Novacart as a customer, an administrator, and a system administrator.
> For setup instructions, see the [README Getting Started](../README.md#getting-started).

---

## Running the app

Start everything with Docker:

```bash
docker compose up --build -d
```

Then open:

| What | URL |
|---|---|
| Storefront (frontend) | http://localhost:3001 *(locally remapped; 3000 in prod compose)* |
| API / Swagger | http://localhost:5000/swagger |
| Health check | http://localhost:5000/api/health |

---

## 1. Customer guide

### Register & log in

1. Click **Sign in** in the top bar → **Register**.
2. Enter your name, email, and password. On success you're automatically logged in.
3. Returning users: **Sign in** with email + password.

Your session is held in an **HttpOnly cookie** — you stay logged in across browser restarts until the token expires or you log out. Logging out clears the cookie.

### Browse & search products

- **Products** page: keyword search (debounced), multi-select category filters, price range, tag chips, and sort (newest / price / name).
- Click a product card to see full details, including a **dynamic specifications table** built from the product's `metadata` (attributes differ per product type — electronics show ports/battery, apparel shows sizes/material, etc.).
- Product images come from Unsplash URLs by default; admins can sync from Square or upload their own.

### Shopping cart

- Click **Add** on a product card or detail page.
- The cart badge in the header shows the item count.
- On the **Cart** page: change quantities (stepper, capped at stock), remove items, see the live subtotal (with dynamic pricing applied).
- **Guest carts**: you can add items before logging in — they're merged into your account cart automatically when you sign in.

### Checkout & payment

1. From the cart, click **Proceed to checkout**.
2. Select a saved shipping address (or manage addresses under **Account**).
3. You're redirected to **Stripe Checkout** (sandbox). Use a test card:
   - Card: `4242 4242 4242 4242`
   - Any future expiry, any CVC, any postcode.
4. On success you land on a confirmation page; cancelling returns you to a cancel page.

> No real charges occur — this is Stripe's sandbox. Novacart never sees or stores card numbers (tokenisation).

### Order history

- **Orders** page lists past purchases with status badges (Pending, Paid, Processing, Shipped, Completed, Cancelled).
- Expand an order to see line items with **frozen prices** (the price you paid at purchase time, unaffected by later price changes).

### Profile, wishlist & addresses

- **Account**: edit your full name (email is read-only).
- **Wishlist**: heart a product from its card/detail to save it; manage saved items on the Wishlist page.
- **Addresses**: add/edit/delete shipping & billing addresses; mark one as default (only one default shipping + one default billing at a time).

### Install as an app (PWA)

The storefront is a **Progressive Web App** — in Chrome/Edge, use the install button in the address bar to add it as a standalone app. It works offline for previously-visited pages (shows an offline page when the network is down).

---

## 2. Administrator guide

> Admin endpoints require the `admin` or `sysadmin` role. In Development, an admin account is auto-created on startup.

### Dev admin bootstrap (Development only)

Configured in `backend/appsettings.Development.json`:

```
Email:    admin@novacart.local
Password: Admin123!
```

Log in at the storefront with these credentials. You'll see an **Admin dashboard** link in the user menu.

### Product management (`/admin/products`)

- **List**: all products (active + inactive), with search, status filter, and pagination.
- **Create / Edit**: name, slug, price, currency, stock quantity, category, metadata (JSON), and image URL.
- **Inventory**: adjust stock; low-stock and out-of-stock badges surface automatically.
- **Deactivate** (soft delete): hides a product from the storefront without deleting its data; can be reactivated.
- **Sync from Square**: pulls the catalogue from the Square sandbox (or a simulated set when no token is set).

### Order management (`/admin/orders`)

- View all orders, search by order number / customer email, filter by status.
- Open an order to see line items and totals.
- **Advance status** through the workflow (validated state machine):
  `pending → paid → processing → shipped → completed`
  Cancel is allowed from `pending` or `paid`.
- Illegal transitions are rejected. Each change is recorded in an audit history with actor + timestamp, and triggers a status-update email to the customer.

### Pricing rules (`/admin/pricing`)

- Create rules: scope (product / category / global), type (percent off / flat off / fixed price), value, active time window, enabled toggle.
- Rules apply at price-read time across the catalogue, cart, and checkout. **Most specific wins** (product > category > global).
- Historical order prices are unaffected (frozen at purchase).

### Analytics (`/admin/analytics`)

- KPIs: total revenue, total orders, units sold, average order value.
- **Sales over time** chart (ECharts) — revenue (line) and orders (bars), with date gaps filled.
- **Best sellers** and **low-stock** tables.

---

## 3. System Administrator guide

> The `sysadmin` role has access to everything an admin does, **plus** system-level operations.

### System diagnostics (`/admin/system`)

Restricted to `sysadmin` only (`[Authorize(Roles = RoleNames.SysAdmin)]`):

- **Health dashboard**: deep DB connectivity + Redis ping status.
- **Clear cache**: flushes the product / order / analytics cache prefixes in Redis. Use after bulk data changes or when stale data is suspected.

> The distinction: `admin` manages *business* data (products, orders, pricing); `sysadmin` manages the *system* (caches, diagnostics). This is the P14 three-role differentiation.

---

## 4. Troubleshooting

| Symptom | Likely cause / fix |
|---|---|
| Can't log in / 401 loop | Cookie blocked by browser; ensure the site is served over `http://localhost` (not an IP) so `SameSite=Strict` applies. |
| Stripe checkout fails | Sandbox keys misconfigured; check `.env` `STRIPE_SECRET_KEY`. Webhook not firing? See [Stripe webhook local testing](STRIPE_WEBHOOK_LOCAL.md). |
| Admin link missing | You're not `admin`/`sysadmin`. Use the dev bootstrap account, or assign the role in the DB. |
| Products won't sync from Square | No access token set; falls back to simulated sync. Set `SQUARE_ACCESS_TOKEN` in `.env`. |
| Stale data after admin changes | Cache TTL is 60s (products) / 30s (orders). A sysadmin can clear it instantly via `/admin/system`. |

---

## Related

- [Architecture](ARCHITECTURE.md) · [UI Design](UI-DESIGN.md) · [Deployment](deployment-guide.md)
