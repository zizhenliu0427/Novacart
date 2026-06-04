# P14 Modern E-commerce — GitHub Issues by Sprint

Total estimated duration: **~13 weeks** (assuming 15–20 hrs/week part-time).
Each Sprint is 1–2 weeks. Every Sprint ends with a deployable, tagged release.

**Labels to create in GitHub first:**
`type:feature` `type:chore` `type:bug` `type:docs` `type:test`
`area:backend` `area:frontend` `area:db` `area:devops` `area:auth` `area:payment`
`priority:p0` `priority:p1` `priority:p2`
`sprint:0` through `sprint:8`

---

## Sprint 0 — Foundation & Setup (Week 1)

**Goal:** A bootable empty skeleton. No business logic yet.
**Release tag at end:** `v0.0.1`

### Issue 0.1 — Initialize monorepo and tech stack
**Labels:** `type:chore` `area:devops` `priority:p0` `sprint:0`
**Description:**
Set up the project repository with the following structure:
```
/backend       (.NET 8 Web API)
/frontend      (Next.js 14 App Router + TypeScript)
/docker        (Docker Compose configs)
/docs          (architecture, API specs)
```
**Acceptance criteria:**
- [ ] `docker compose up` brings up Postgres, Redis, backend, frontend
- [ ] Backend `/health` endpoint returns 200
- [ ] Frontend renders a placeholder home page
- [ ] README has setup instructions

---

### Issue 0.2 — Configure CI/CD pipeline skeleton
**Labels:** `type:chore` `area:devops` `priority:p0` `sprint:0`
**Description:**
GitHub Actions workflow for build + lint + test on every PR.
**Acceptance criteria:**
- [ ] `.github/workflows/ci.yml` runs `dotnet build`, `dotnet test`, `npm run build`, `npm run lint`
- [ ] PR cannot merge if CI fails (branch protection rule)
- [ ] Workflow runs in under 5 minutes

---

### Issue 0.3 — Decide and document architecture
**Labels:** `type:docs` `priority:p0` `sprint:0`
**Description:**
Write `docs/ARCHITECTURE.md` covering: layering (Clean Architecture / Vertical Slice), folder convention, dependency injection setup, error handling strategy.
**Acceptance criteria:**
- [ ] Architecture decision recorded with rationale
- [ ] Diagram included (Mermaid)

---

## Sprint 1 — Auth & User Foundation (Weeks 2–3)

**Goal:** Users can sign up, log in, manage profile. Multi-role system in place.
**Release tag:** `v0.1.0`

### Issue 1.1 — Design multi-role user schema
**Labels:** `type:feature` `area:db` `area:auth` `priority:p0` `sprint:1`
**Description:**
Create migrations for `users`, `roles`, `user_roles`, `user_profiles` tables.
Seed `roles` table with: `customer`, `admin`, `sysadmin`.
**Acceptance criteria:**
- [ ] EF Core migrations created and runnable
- [ ] Seed data inserted on app startup if missing
- [ ] User can have multiple roles (many-to-many)

---

### Issue 1.2 — Implement registration endpoint
**Labels:** `type:feature` `area:backend` `area:auth` `priority:p0` `sprint:1`
**Description:**
`POST /api/auth/register` with email + password. Password hashed with bcrypt/argon2. Default role: `customer`.
**Acceptance criteria:**
- [ ] Validates email format and password strength
- [ ] Returns 409 if email exists
- [ ] Returns 201 with user ID on success
- [ ] Unit tests for happy path + 3 edge cases

---

### Issue 1.3 — Implement login + JWT issuance
**Labels:** `type:feature` `area:backend` `area:auth` `priority:p0` `sprint:1`
**Description:**
`POST /api/auth/login` returns access token (15 min) + refresh token (7 days). Include user roles as JWT claims.
**Acceptance criteria:**
- [ ] JWT signed with HS256 (or RS256 if going production-grade)
- [ ] Refresh token endpoint `POST /api/auth/refresh`
- [ ] Logout endpoint invalidates refresh token (Redis blocklist)

---

### Issue 1.4 — Frontend auth pages
**Labels:** `type:feature` `area:frontend` `area:auth` `priority:p0` `sprint:1`
**Description:**
Build `/login`, `/register`, `/profile` pages with Tailwind. Use Next.js middleware to guard `/profile`.
**Acceptance criteria:**
- [ ] Form validation with client + server-side errors displayed
- [ ] Tokens stored in HttpOnly cookies (not localStorage)
- [ ] Auto-refresh on 401

---

### Issue 1.5 — Profile management
**Labels:** `type:feature` `area:backend` `area:frontend` `priority:p1` `sprint:1`
**Description:**
Customer can update name, phone, addresses. Admin can view/edit any user.
**Acceptance criteria:**
- [ ] `GET /api/users/me` and `PATCH /api/users/me`
- [ ] Address as separate entity (one user → many addresses)
- [ ] UI on `/profile` page

---

## Sprint 2 — Product Catalogue (Weeks 4–5)

**Goal:** Customers can browse, search, filter products. Admin can manage products.
**Release tag:** `v0.2.0`

### Issue 2.1 — Product schema with tags and metadata
**Labels:** `type:feature` `area:db` `priority:p0` `sprint:2`
**Description:**
Tables: `products`, `categories`, `product_images`, `product_tags`. Products have a `metadata JSONB` column for product-type-specific attributes (size, color, etc.) per P14 spec.
**Acceptance criteria:**
- [ ] Migrations created
- [ ] Index on `(category_id, is_active)` and GIN index on `tags` array
- [ ] Seed 30 fake products across 5 categories

---

### Issue 2.2 — Admin: Product CRUD endpoints
**Labels:** `type:feature` `area:backend` `priority:p0` `sprint:2`
**Description:**
Authenticated admin-only endpoints to create/read/update/delete products.
**Acceptance criteria:**
- [ ] Authorization check: requires `admin` role claim
- [ ] Image upload to S3-compatible storage (use MinIO locally)
- [ ] Soft delete (set `is_active = false`)

---

### Issue 2.3 — Customer: Product browsing API
**Labels:** `type:feature` `area:backend` `priority:p0` `sprint:2`
**Description:**
`GET /api/products` with query params: `category`, `tags`, `minPrice`, `maxPrice`, `sort`, `page`, `pageSize`, `q` (keyword).
**Acceptance criteria:**
- [ ] Pagination metadata in response
- [ ] Keyword search uses PostgreSQL full-text search (not LIKE)
- [ ] Response cached in Redis for 60 seconds (cache key includes all filters)

---

### Issue 2.4 — Frontend: Product list and detail pages
**Labels:** `type:feature` `area:frontend` `priority:p0` `sprint:2`
**Description:**
`/products` (list with filters, sort dropdown), `/products/[slug]` (detail).
**Acceptance criteria:**
- [ ] Filter UI updates URL query params (shareable links)
- [ ] Skeleton loading states
- [ ] SEO meta tags via Next.js metadata API

---

### Issue 2.5 — Admin: Product management UI
**Labels:** `type:feature` `area:frontend` `priority:p1` `sprint:2`
**Description:**
`/admin/products` page with table, search, bulk actions, edit modal.
**Acceptance criteria:**
- [ ] Image drag-and-drop upload
- [ ] Inline edit for price and stock
- [ ] Only visible to admin role

---

## Sprint 3 — Cart & Wishlist (Weeks 6–7)

**Goal:** Persistent cart for both guests and logged-in users, with merge-on-login.
**Release tag:** `v0.3.0`

### Issue 3.1 — Cart schema and guest cart strategy
**Labels:** `type:feature` `area:db` `area:backend` `priority:p0` `sprint:3`
**Description:**
`carts` table with nullable `user_id` and nullable `session_id`. `cart_items` table snapshots `price_at_add`.
**Acceptance criteria:**
- [ ] Guest carts identified by signed session cookie
- [ ] User carts identified by user_id
- [ ] Constraint: cart must have either user_id or session_id

---

### Issue 3.2 — Cart CRUD endpoints
**Labels:** `type:feature` `area:backend` `priority:p0` `sprint:3`
**Description:**
`GET/POST/PATCH/DELETE /api/cart` and `/api/cart/items`.
**Acceptance criteria:**
- [ ] Quantity validation against stock
- [ ] Returns recalculated totals on every mutation
- [ ] Out-of-stock items flagged but not auto-removed

---

### Issue 3.3 — Cart merge on login
**Labels:** `type:feature` `area:backend` `area:auth` `priority:p0` `sprint:3`
**Description:**
On successful login, if a guest cart exists in cookie, merge its items into the user cart. Same product → sum quantities (capped by stock).
**Acceptance criteria:**
- [ ] Triggered automatically post-login
- [ ] Idempotent (re-running merge produces same result)
- [ ] Unit tests for: only guest cart, only user cart, both, both with overlapping products

---

### Issue 3.4 — Frontend: Cart drawer and page
**Labels:** `type:feature` `area:frontend` `priority:p0` `sprint:3`
**Description:**
Cart drawer (slide-in panel) for quick view, `/cart` page for full review. Update quantity, remove, view subtotal.
**Acceptance criteria:**
- [ ] Optimistic UI updates
- [ ] Works for both guest and logged-in user
- [ ] Empty state with CTA

---

### Issue 3.5 — Wishlist
**Labels:** `type:feature` `area:backend` `area:frontend` `priority:p1` `sprint:3`
**Description:**
Logged-in users can add/remove products from wishlist. `/wishlist` page.
**Acceptance criteria:**
- [ ] `POST/DELETE /api/wishlist/{productId}`
- [ ] Heart icon toggles on product cards
- [ ] Move-to-cart action

---

## Sprint 4 — Checkout & Stripe (Weeks 8–9)

**Goal:** End-to-end purchase flow with Stripe sandbox and webhooks.
**Release tag:** `v0.4.0` ← **This is when project becomes resume-worthy**

### Issue 4.1 — Payment provider abstraction layer
**Labels:** `type:feature` `area:backend` `area:payment` `priority:p0` `sprint:4`
**Description:**
Define `IPaymentProvider` interface with methods: `CreateCheckoutSession`, `VerifyWebhookSignature`, `HandleEvent`. Implement `StripePaymentProvider`. Register via DI.
**Acceptance criteria:**
- [ ] Interface lives in domain layer (no Stripe SDK dependency)
- [ ] Implementation in infrastructure layer
- [ ] Unit tests with mocked Stripe responses

---

### Issue 4.2 — Stripe Checkout Session creation
**Labels:** `type:feature` `area:backend` `area:payment` `priority:p0` `sprint:4`
**Description:**
`POST /api/checkout/session` creates a Stripe Checkout Session from current cart. Returns session URL.
**Acceptance criteria:**
- [ ] Cart frozen at session creation (price snapshot)
- [ ] Order created in `pending` state before redirect
- [ ] Success and cancel URLs configured

---

### Issue 4.3 — Stripe webhook handler with idempotency
**Labels:** `type:feature` `area:backend` `area:payment` `priority:p0` `sprint:4`
**Description:**
`POST /api/webhooks/stripe` handles `checkout.session.completed`, `payment_intent.payment_failed`, `charge.refunded`. Verify signature. Store every webhook event in `payment_webhooks` table with idempotency key.
**Acceptance criteria:**
- [ ] Reject requests with invalid signature (400)
- [ ] Duplicate event ID is no-op (idempotency)
- [ ] Successful payment transitions order to `paid` state
- [ ] Failed event saved with `processed = false` for manual retry

---

### Issue 4.4 — Local webhook testing with ngrok
**Labels:** `type:chore` `type:docs` `area:devops` `priority:p1` `sprint:4`
**Description:**
Document the ngrok flow in `docs/STRIPE_WEBHOOK_LOCAL.md`. Include script to start ngrok and auto-update Stripe dashboard webhook URL.
**Acceptance criteria:**
- [ ] Step-by-step instructions
- [ ] Helper script in `/scripts/start-ngrok.sh`

---

### Issue 4.5 — Checkout UI flow
**Labels:** `type:feature` `area:frontend` `priority:p0` `sprint:4`
**Description:**
`/checkout` page: review cart → enter shipping address → redirect to Stripe Checkout → success/cancel landing pages.
**Acceptance criteria:**
- [ ] Address form with validation
- [ ] Loading state during Stripe redirect
- [ ] Success page polls order status until webhook completes

---

### Issue 4.6 — Order confirmation email
**Labels:** `type:feature` `area:backend` `priority:p1` `sprint:4`
**Description:**
Send email when order transitions to `paid`. Use background service + Redis queue.
**Acceptance criteria:**
- [ ] Email queued, not sent synchronously
- [ ] HTML template with order summary
- [ ] Use MailHog locally, SendGrid in prod

---

## Sprint 5 — Orders & State Machine (Weeks 10–11)

**Goal:** Order lifecycle managed via configurable state machine.
**Release tag:** `v0.5.0`

### Issue 5.1 — Order state machine table design
**Labels:** `type:feature` `area:db` `area:backend` `priority:p0` `sprint:5`
**Description:**
Create `order_status_transitions` table defining: `from_status`, `to_status`, `required_role`. Seed with: `pending→paid` (system), `paid→processing` (admin), `processing→shipped` (admin), `shipped→completed` (system/customer), any→`cancelled` (admin or system).
**Acceptance criteria:**
- [ ] Transitions enforced via database query, not hardcoded
- [ ] `order_status_history` table logs every transition with actor and timestamp
- [ ] Invalid transition returns 422 with reason

---

### Issue 5.2 — Order history view (customer)
**Labels:** `type:feature` `area:frontend` `area:backend` `priority:p0` `sprint:5`
**Description:**
`/orders` page lists user's orders with current status, total, date. `/orders/[id]` shows detail with status timeline.
**Acceptance criteria:**
- [ ] Pagination (10 per page)
- [ ] Cancel button visible only when state allows
- [ ] Timeline UI shows transitions with timestamps

---

### Issue 5.3 — Admin order management
**Labels:** `type:feature` `area:frontend` `area:backend` `priority:p0` `sprint:5`
**Description:**
`/admin/orders` page with filters by status, search by order ID, bulk status update.
**Acceptance criteria:**
- [ ] Only valid next-states shown in status dropdown (driven by transition table)
- [ ] Status change triggers notification email

---

### Issue 5.4 — Cache order list in Redis
**Labels:** `type:feature` `area:backend` `priority:p2` `sprint:5`
**Description:**
Cache `GET /api/orders/me` for 30 seconds. Invalidate on order state change.
**Acceptance criteria:**
- [ ] Cache hit rate logged (metric)
- [ ] Invalidation correctness tested

---

## Sprint 6 — Admin Analytics Dashboard (Weeks 12–13)

**Goal:** Admin sees business metrics at a glance.
**Release tag:** `v0.6.0`

### Issue 6.1 — Analytics aggregation queries
**Labels:** `type:feature` `area:backend` `area:db` `priority:p0` `sprint:6`
**Description:**
Endpoints: total sales, orders per day (last 30 days), revenue summary, top 10 best-selling products.
**Acceptance criteria:**
- [ ] Queries use date_trunc for grouping
- [ ] Results cached for 5 minutes
- [ ] Performance: each query under 200ms with 10k orders

---

### Issue 6.2 — Inventory tracking view
**Labels:** `type:feature` `area:frontend` `priority:p1` `sprint:6`
**Description:**
Low-stock alert list, products by stock level, restock suggestions.
**Acceptance criteria:**
- [ ] Threshold configurable per product
- [ ] Visual indicators (red/yellow/green)

---

### Issue 6.3 — Dashboard UI
**Labels:** `type:feature` `area:frontend` `priority:p0` `sprint:6`
**Description:**
`/admin/dashboard` page with cards, line chart (revenue over time), bar chart (top products).
**Acceptance criteria:**
- [ ] Use Recharts or Chart.js
- [ ] Date range picker (7d / 30d / 90d / custom)
- [ ] Export to CSV button

---

## Sprint 7 — PWA & Mobile Polish (Week 14)

**Goal:** Installable, offline-capable, mobile-first.
**Release tag:** `v0.7.0`

### Issue 7.1 — Web app manifest
**Labels:** `type:feature` `area:frontend` `priority:p0` `sprint:7`
**Description:**
Add `manifest.json` with app name, icons (multiple sizes), theme color, display mode `standalone`.
**Acceptance criteria:**
- [ ] Passes Lighthouse PWA audit (installable)
- [ ] Icons render correctly on iOS and Android

---

### Issue 7.2 — Service worker for offline browsing
**Labels:** `type:feature` `area:frontend` `priority:p0` `sprint:7`
**Description:**
Cache product list and detail pages. Show "you're offline" banner when API unreachable.
**Acceptance criteria:**
- [ ] Cache-first strategy for static assets
- [ ] Network-first with fallback for product pages
- [ ] Service worker version bumped on each release

---

### Issue 7.3 — Mobile responsive audit
**Labels:** `type:chore` `area:frontend` `priority:p1` `sprint:7`
**Description:**
Test all pages on iPhone SE, iPhone 14, iPad, Pixel viewport widths. Fix layout breaks.
**Acceptance criteria:**
- [ ] No horizontal scroll on any page
- [ ] Touch targets minimum 44x44px

---

## Sprint 8 — Testing, Docs, Deploy (Week 15+)

**Goal:** Production-grade quality and shipped to AWS.
**Release tag:** `v1.0.0` ← **First production release**

### Issue 8.1 — End-to-end test for purchase flow
**Labels:** `type:test` `priority:p0` `sprint:8`
**Description:**
Playwright test: register → browse → add to cart → checkout (Stripe test card) → see order in history.
**Acceptance criteria:**
- [ ] Runs in CI
- [ ] Includes negative path (declined card)

---

### Issue 8.2 — Unit test coverage on critical paths
**Labels:** `type:test` `priority:p0` `sprint:8`
**Description:**
Minimum coverage: auth (80%), order state machine (90%), payment webhook handler (90%).
**Acceptance criteria:**
- [ ] Coverage report in CI
- [ ] No critical path untested

---

### Issue 8.3 — Swagger / OpenAPI documentation
**Labels:** `type:docs` `priority:p1` `sprint:8`
**Description:**
Annotate every endpoint. Host Swagger UI at `/api/docs`.
**Acceptance criteria:**
- [ ] All endpoints have request/response examples
- [ ] Authentication documented

---

### Issue 8.4 — AWS deployment
**Labels:** `type:chore` `area:devops` `priority:p0` `sprint:8`
**Description:**
Deploy: backend on ECS Fargate (or EC2 + Docker), frontend on Vercel/Amplify, Postgres on RDS, Redis on ElastiCache, S3 for images.
**Acceptance criteria:**
- [ ] Infrastructure as code (Terraform or AWS CDK)
- [ ] HTTPS with ACM cert
- [ ] Custom domain configured

---

### Issue 8.5 — README with architecture and demo video
**Labels:** `type:docs` `priority:p0` `sprint:8`
**Description:**
Write production-quality README. Record 3–5 minute demo video. Upload to YouTube.
**Acceptance criteria:**
- [ ] Architecture diagram (Mermaid)
- [ ] Tech stack with rationale
- [ ] Setup instructions tested on fresh machine
- [ ] Demo video linked at top

---

## Milestones summary

| Milestone | Tag | Cumulative weeks | Resume-ready? |
|---|---|---|---|
| Foundation | v0.0.1 | 1 | No |
| Auth | v0.1.0 | 3 | No |
| Catalogue | v0.2.0 | 5 | No |
| Cart | v0.3.0 | 7 | No |
| **Checkout** | **v0.4.0** | **9** | **Yes — minimum viable resume project** |
| Orders | v0.5.0 | 11 | Yes |
| Admin Analytics | v0.6.0 | 13 | Yes — strong resume project |
| PWA | v0.7.0 | 14 | Yes |
| **Production** | **v1.0.0** | **15+** | **Yes — top-tier resume project** |

After v1.0.0, optionally proceed to Phase 3 features (membership/subscriptions from P102, recommendations, fraud rules).
