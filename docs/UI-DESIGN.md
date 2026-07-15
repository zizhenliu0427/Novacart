# Novacart — UI Design System

> The visual language and component library that all Novacart interfaces build against.
> Source of truth: [`frontend/src/app/globals.css`](../frontend/src/app/globals.css) (tokens) and [`frontend/src/components/ui/`](../frontend/src/components/ui/) (components).

---

## 1. Design principles

Novacart is a **general-purpose, multi-category e-commerce platform** — it sells *any* product type (electronics, apparel, home goods, …) and labels each with type-specific attributes loaded dynamically from `metadata`. Because the catalogue carries every kind of product, the UI is deliberately:

- **Neutral & content-first** — the chrome stays quiet so that **product imagery carries the color**. A clean, trustworthy retail surface that adapts to whatever is being sold.
- **Token-driven** — every colour, shadow, and border comes from a CSS custom property. A full rebrand = changing `--accent` (plus its hover/weak variants) in one place.
- **Accessible by default** — visible focus rings, keyboard-operable modals (focus trap + Escape), `prefers-reduced-motion` respected, semantic roles/labels.
- **Theme-adaptive** — light is the default; dark follows the OS automatically via `prefers-color-scheme`.

---

## 2. Design tokens

All tokens are CSS custom properties defined in `globals.css`, consumed by Tailwind via `tailwind.config.ts`. Light values live on `:root`; dark values in `@media (prefers-color-scheme: dark)`.

### Surface & ink

| Token | Light | Dark | Tailwind class |
|---|---|---|---|
| `--bg` | `#ffffff` | `#0b0e14` | `bg-bg` |
| `--bg-subtle` | `#f5f6f8` | `#131722` | `bg-bg-subtle` |
| `--surface` | `#ffffff` | `#131722` | `bg-surface` |
| `--border` | `#e5e7eb` | `#262b36` | `border` (default border colour) |
| `--ink` | `#111827` | `#e5e7eb` | `text-ink` |
| `--ink-muted` | `#6b7280` | `#9ca3af` | `text-ink-muted` |

### Accent (rebrand point)

| Token | Light | Dark |
|---|---|---|
| `--accent` | `#2563eb` | `#3b82f6` |
| `--accent-hover` | `#1d4ed8` | `#60a5fa` |
| `--accent-weak` | `#eff4ff` | `#17233b` |
| `--accent-contrast` | `#ffffff` | `#ffffff` |

### Status colours

| Token | Light | Dark | Use |
|---|---|---|---|
| `--success` | `#16a34a` | `#22c55e` | healthy / paid |
| `--warning` | `#d97706` | `#f59e0b` | low stock |
| `--danger` | `#dc2626` | `#f87171` | errors / out-of-stock |
| `--sale` | `#dc2626` | `#f87171` | discount badges |
| `--rating` | `#f59e0b` | `#fbbf24` | star ratings |

### Shadows

| Token | Light | Dark |
|---|---|---|
| `--shadow-card` | `0 1px 2px rgba(17,24,39,0.06)` | `0 1px 2px rgba(0,0,0,0.4)` |
| `--shadow-hover` | `0 4px 16px rgba(17,24,39,0.1)` | `0 6px 20px rgba(0,0,0,0.55)` |

---

## 3. Typography

- **Font**: [Inter](https://rsms.me/inter/), loaded via `next/font/google` with `display: 'swap'` and exposed as the `--font-sans` variable. Falls back to `system-ui, -apple-system, "Segoe UI", Roboto, sans-serif`.
- **Body**: `font-family: var(--font-sans)`, `-webkit-font-smoothing: antialiased`.
- **Tabular numbers**: a `.tnum` utility (`font-variant-numeric: tabular-nums`) is applied wherever prices or quantities are displayed so digits align in columns.

---

## 4. Motion

Animations are minimal and purposeful. `globals.css` defines three keyframes:

| Keyframe | Effect | Used by |
|---|---|---|
| `fadeIn` | opacity 0 → 1 | modal overlay, toast |
| `scaleIn` | `scale(0.95) translateY(-8px)` → 1 | modal panel |
| `slideInRight` | `translateX(100%)` → 0 | toast notifications |

**Accessibility:** `@media (prefers-reduced-motion: reduce)` forces all animation/transition durations to `0.01ms !important`.

---

## 5. Component library

All primitives live in [`frontend/src/components/ui/`](../frontend/src/components/ui/). Each is a controlled, `forwardRef` component written in TypeScript.

### Button
`variant: 'primary' | 'secondary' | 'ghost'` · `size: 'sm' | 'md'`

| Variant | Style |
|---|---|
| `primary` | `bg-accent text-accent-contrast hover:bg-accent-hover` |
| `secondary` | `bg-surface text-ink border hover:bg-bg-subtle` |
| `ghost` | `text-ink-muted hover:bg-bg-subtle hover:text-ink` |

Disabled state: `cursor-not-allowed opacity-40`. Base: `rounded-lg font-semibold transition`.

### Input
`label?` · `error?` · `helperText?` · `size: 'sm' | 'md' | 'lg'`

Auto-generates an id (`useId`) and binds `<label htmlFor>`. Error state switches the border to `--danger` and shows the message below; otherwise shows `helperText`. Label is `uppercase tracking-wider text-xs`.

### Card
`interactive?: boolean` (default false)

`rounded-xl border bg-surface shadow-card`. When `interactive`, adds `hover:-translate-y-0.5 hover:shadow-hover` — used for product cards.

### Badge
`tone: 'neutral' | 'accent' | 'success' | 'warning' | 'danger' | 'sale'`

`rounded-full px-2.5 py-0.5 text-xs`. `sale` tone uses a solid `bg-sale text-white`; others use `bg-bg-subtle` with the matching status colour for text.

### Modal
`open` · `onClose` · `title?` · `children` · `footer?`

Rendered via `createPortal` to `document.body`. **Fully keyboard-accessible**: focus trap (Tab cycles within), Escape to close, focus restored to the trigger on close, body scroll locked while open. Background click closes. Animates in with `fadeIn` (overlay) + `scaleIn` (panel).

### DataTable `<T>` (generic)
`columns: Column<T>[]` · `data` · `loading?` · `skeletonRows?` (default 5) · `emptyMessage?`

A column has `key`, `header`, optional `render(row, index)`, and `align`. Responsive: wrapped in `overflow-x-auto`, table `min-w-[640px]`. Shows skeleton rows while loading, an empty-state message when `data` is empty.

### Pagination
`page` · `totalPages` · `onPageChange`

Hides itself when `totalPages <= 1`. Shows first/last, current ±1, with `…` ellipsis for large ranges (windowed). Current page marked with `aria-current="page"`.

### Toast
`variant: 'success' | 'error' | 'info'` · `message` · `onDismiss`

Fixed top-right stack, managed by `ToastContext`. Each toast has a coloured left border + circular icon (`✓` / `✕` / `ℹ`). Slides in with `slideInRight`, auto-dismissed by the context.

### EmptyState
`icon` · `title` · `description?` · `action?`

Centred card with a large icon in a muted circle, used for empty carts, no-results, and "sign in required" states.

### ComingSoon
`title` · `item` · `children?`

A placeholder card for roadmap-tracked features, labelled with an accent badge. Keeps the UI honest about what is scaffolded vs. shipped.

---

## 6. Iconography

A **zero-dependency** icon set in [`components/icons.tsx`](../frontend/src/components/icons.tsx): SVGs with `stroke="currentColor"`, `strokeWidth 1.6`, `viewBox 0 0 24 24`, `aria-hidden`. Includes `Cart`, `Package`, `Grid`, `Spark`, `User`, `Trash`, `Plus`, `Minus`, `Menu`, `Close`, `Heart`, `HeartFilled`. Because they use `currentColor`, icons automatically inherit the text colour and follow theme switching.

---

## 7. Responsive strategy

Tailwind default breakpoints (no custom `screens`): `sm 640` · `md 768` · `lg 1024` · `xl 1280`.

### Layout patterns

| Pattern | Classes | Where |
|---|---|---|
| Product grid (4→3→2→1) | `grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4` | products list, wishlist |
| Detail two-column | `grid gap-8 lg:grid-cols-2` | product detail |
| List + filter sidebar | `grid gap-8 lg:grid-cols-[240px_1fr]` | products page |
| Cart/checkout (main + summary) | `grid gap-6 lg:grid-cols-3` | cart, checkout |
| Admin KPI cards | `grid gap-4 sm:grid-cols-2 lg:grid-cols-4` | analytics |
| Form fields | `grid gap-4 sm:grid-cols-2` | admin create/edit forms |

- **Units**: `rem`-based (Tailwind spacing) throughout — no hardcoded pixels for layout.
- **Container**: content is centred at `max-w-content` (80rem / 1280px) with `px-4 sm:px-6` gutters.
- **Header**: sticky top bar collapses the nav into a hamburger menu below `sm` (640px).
- **Admin sidebar**: fixed 180px on `md+`, collapses into a slide-down panel below `md` (768px) — the README's "auto-collapse at ≤768px".

---

## 8. Page structure

### Customer side

A single centred column (`<main className="mx-auto max-w-content px-4 py-8 sm:px-6">`) with a global sticky `HeaderNav` (logo, nav links, cart badge, user menu, mobile hamburger). No persistent sidebar; pages like *products* and *checkout* define their own local multi-column grids as needed. Footer is fixed across all pages.

### Admin side

`/admin` has a nested `layout.tsx` providing a sidebar (Dashboard, Products, Orders, Pricing, Analytics, + System for sysadmin) alongside the content area. Client-side role gate hides everything for non-admins; real authorisation is enforced by the backend `[Authorize(Roles)]` and the Edge middleware.

---

## 9. PWA surface

- **Manifest** (`/manifest.json`): `display: standalone`, `start_url: /`, theme colours, icons → installable.
- **Service Worker** (`/sw.js`): precaches the app shell, network-first for navigations with `/offline` fallback, cache-first for static assets, skips `/api/` requests.
- **Offline page** (`/offline`): a friendly retry prompt shown when a navigation fails while offline.

---

## 10. Theming & rebranding

To rebrand Novacart, edit only the accent tokens in `globals.css`:

```css
:root {
  --accent: #<your-brand>;
  --accent-hover: #<darker>;
  --accent-weak: #<tint>;
}
@media (prefers-color-scheme: dark) {
  :root {
    --accent: #<dark-mode-brand>;
    /* … */
  }
}
```

Because every component references `var(--accent)` (via Tailwind's `accent` colour mapping), the entire UI rebrands with no component edits. The ECharts dashboard (`SalesChart.tsx`) also reads `var(--accent)` so charts follow the theme.
