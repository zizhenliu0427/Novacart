# Novacart — UI 设计系统

> 英文版:[UI-DESIGN.md](UI-DESIGN.md)

> 所有 Novacart 界面共同遵循的视觉语言与组件库。
> 真相来源(source of truth):[`frontend/src/app/globals.css`](../frontend/src/app/globals.css)(token)与 [`frontend/src/components/ui/`](../frontend/src/components/ui/)(组件)。

---

## 1. 设计原则

Novacart 是一个**通用型、全品类的电商平台**——它销售*任意*类型商品(电子产品、服装、家居用品……),并为每件商品打上类型专属的属性标签,这些属性从 `metadata` 动态加载。由于商品目录涵盖各类商品,UI 在设计上刻意遵循:

- **中性、内容优先**——界面外框保持安静,让**商品图片承担色彩**。一个干净、值得信赖的零售界面,能适应任何在售商品。
- **Token 驱动(Token-driven)**——每种颜色、阴影、边框都来自一个 CSS custom property(自定义属性)。一次完整的品牌重塑(rebrand)=只需在一处修改 `--accent`(及其 hover / weak 变体)。
- **默认无障碍**——可见的聚焦环(focus ring)、可用键盘操作的模态框(focus trap + Escape)、尊重 `prefers-reduced-motion`、使用语义化角色 / 标签。
- **主题自适应**——默认浅色;深色模式通过 `prefers-color-scheme` 跟随操作系统自动切换。

---

## 2. 设计 token

所有 token 都是定义在 `globals.css` 中的 CSS custom property,通过 `tailwind.config.ts` 由 Tailwind 消费。浅色值位于 `:root`;深色值位于 `@media (prefers-color-scheme: dark)`。

### 表面与文字(Surface & ink)

| Token | Light | Dark | Tailwind class |
|---|---|---|---|
| `--bg` | `#ffffff` | `#0b0e14` | `bg-bg` |
| `--bg-subtle` | `#f5f6f8` | `#131722` | `bg-bg-subtle` |
| `--surface` | `#ffffff` | `#131722` | `bg-surface` |
| `--border` | `#e5e7eb` | `#262b36` | `border`(默认边框色) |
| `--ink` | `#111827` | `#e5e7eb` | `text-ink` |
| `--ink-muted` | `#6b7280` | `#9ca3af` | `text-ink-muted` |

### 主题强调色(品牌重塑点)

| Token | Light | Dark |
|---|---|---|
| `--accent` | `#2563eb` | `#3b82f6` |
| `--accent-hover` | `#1d4ed8` | `#60a5fa` |
| `--accent-weak` | `#eff4ff` | `#17233b` |
| `--accent-contrast` | `#ffffff` | `#ffffff` |

### 状态色(Status colours)

| Token | Light | Dark | 用途 |
|---|---|---|---|
| `--success` | `#16a34a` | `#22c55e` | 健康 / 已支付 |
| `--warning` | `#d97706` | `#f59e0b` | 库存不足 |
| `--danger` | `#dc2626` | `#f87171` | 错误 / 缺货 |
| `--sale` | `#dc2626` | `#f87171` | 折扣徽标 |
| `--rating` | `#f59e0b` | `#fbbf24` | 星级评分 |

### 阴影(Shadows)

| Token | Light | Dark |
|---|---|---|
| `--shadow-card` | `0 1px 2px rgba(17,24,39,0.06)` | `0 1px 2px rgba(0,0,0,0.4)` |
| `--shadow-hover` | `0 4px 16px rgba(17,24,39,0.1)` | `0 6px 20px rgba(0,0,0,0.55)` |

---

## 3. 排版(Typography)

- **字体**: [Inter](https://rsms.me/inter/),通过 `next/font/google` 加载,设置 `display: 'swap'`,并以 `--font-sans` 变量暴露。回退到 `system-ui, -apple-system, "Segoe UI", Roboto, sans-serif`。
- **正文**: `font-family: var(--font-sans)`,`-webkit-font-smoothing: antialiased`。
- **等宽数字(Tabular numbers)**:一个 `.tnum` 工具类(`font-variant-numeric: tabular-nums`)被应用于所有显示价格或数量的位置,以保证数字在列中对齐。

---

## 4. 动效(Motion)

动画克制而有意为之。`globals.css` 定义了三个关键帧(keyframe):

| Keyframe | 效果 | 使用者 |
|---|---|---|
| `fadeIn` | opacity 0 → 1 | 模态框遮罩、toast |
| `scaleIn` | `scale(0.95) translateY(-8px)` → 1 | 模态框面板 |
| `slideInRight` | `translateX(100%)` → 0 | toast 通知 |

**无障碍:** `@media (prefers-reduced-motion: reduce)` 会将所有动画 / 过渡时长强制设为 `0.01ms !important`。

---

## 5. 组件库(Component library)

所有基础组件位于 [`frontend/src/components/ui/`](../frontend/src/components/ui/)。每一个都是受控的、基于 `forwardRef` 的 TypeScript 组件。

### Button
`variant: 'primary' | 'secondary' | 'ghost'` · `size: 'sm' | 'md'`

| Variant | 样式 |
|---|---|
| `primary` | `bg-accent text-accent-contrast hover:bg-accent-hover` |
| `secondary` | `bg-surface text-ink border hover:bg-bg-subtle` |
| `ghost` | `text-ink-muted hover:bg-bg-subtle hover:text-ink` |

禁用态:`cursor-not-allowed opacity-40`。基础样式:`rounded-lg font-semibold transition`。

### Input
`label?` · `error?` · `helperText?` · `size: 'sm' | 'md' | 'lg'`

自动生成 id(`useId`)并绑定 `<label htmlFor>`。错误态会将边框切换为 `--danger`,并在下方显示错误信息;否则显示 `helperText`。标签样式为 `uppercase tracking-wider text-xs`。

### Card
`interactive?: boolean`(默认 false)

`rounded-xl border bg-surface shadow-card`。当 `interactive` 时,添加 `hover:-translate-y-0.5 hover:shadow-hover`——用于商品卡片。

### Badge
`tone: 'neutral' | 'accent' | 'success' | 'warning' | 'danger' | 'sale'`

`rounded-full px-2.5 py-0.5 text-xs`。`sale` tone 使用实心 `bg-sale text-white`;其余使用 `bg-bg-subtle` 配以对应的状态色作为文字颜色。

### Modal
`open` · `onClose` · `title?` · `children` · `footer?`

通过 `createPortal` 渲染到 `document.body`。**完全支持键盘操作**:focus trap(Tab 在内部循环)、Escape 关闭、关闭时焦点恢复到触发器、打开期间锁定 body 滚动。点击背景可关闭。进场动画为 `fadeIn`(遮罩)+ `scaleIn`(面板)。

### DataTable `<T>`(泛型)
`columns: Column<T>[]` · `data` · `loading?` · `skeletonRows?`(默认 5)· `emptyMessage?`

一个 column 含有 `key`、`header`、可选的 `render(row, index)` 以及 `align`。响应式:外层包裹 `overflow-x-auto`,表格 `min-w-[640px]`。加载时显示骨架行,`data` 为空时显示空态提示信息。

### Pagination
`page` · `totalPages` · `onPageChange`

当 `totalPages <= 1` 时隐藏自身。显示首 / 尾页、当前页 ±1,并在跨度较大时使用 `…` 省略号(滑动窗口式)。当前页用 `aria-current="page"` 标记。

### Toast
`variant: 'success' | 'error' | 'info'` · `message` · `onDismiss`

固定在右上角的堆叠,由 `ToastContext` 管理。每个 toast 带有一条彩色左边框 + 圆形图标(`✓` / `✕` / `ℹ`)。通过 `slideInRight` 滑入,由 context 自动消失。

### EmptyState
`icon` · `title` · `description?` · `action?`

居中卡片,含一个置于浅色圆圈中的大图标,用于空购物车、无搜索结果、"需要登录"等状态。

### ComingSoon
`title` · `item` · `children?`

为纳入路线图的功能准备的占位卡片,以 accent 徽标标注。用于如实区分界面中哪些是已搭建脚手架、哪些是已正式发布的功能。

---

## 6. 图标(Iconography)

一个**零依赖**的图标集,位于 [`components/icons.tsx`](../frontend/src/components/icons.tsx):SVG 属性为 `stroke="currentColor"`、`strokeWidth 1.6`、`viewBox 0 0 24 24`、`aria-hidden`。包含 `Cart`、`Package`、`Grid`、`Spark`、`User`、`Trash`、`Plus`、`Minus`、`Menu`、`Close`、`Heart`、`HeartFilled`。由于使用 `currentColor`,图标会自动继承文字颜色,并随主题切换而变化。

---

## 7. 响应式策略

Tailwind 默认断点(未自定义 `screens`):`sm 640` · `md 768` · `lg 1024` · `xl 1280`。

### 布局模式

| 模式 | 类名 | 使用位置 |
|---|---|---|
| 商品网格(4→3→2→1) | `grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4` | 商品列表、心愿单 |
| 详情双栏 | `grid gap-8 lg:grid-cols-2` | 商品详情 |
| 列表 + 筛选侧栏 | `grid gap-8 lg:grid-cols-[240px_1fr]` | 商品页 |
| 购物车 / 结账(主区 + 汇总) | `grid gap-6 lg:grid-cols-3` | 购物车、结账 |
| 后台 KPI 卡片 | `grid gap-4 sm:grid-cols-2 lg:grid-cols-4` | 数据分析 |
| 表单字段 | `grid gap-4 sm:grid-cols-2` | 后台新增 / 编辑表单 |

- **单位**:全程使用基于 `rem` 的(Tailwind spacing)——布局中不使用硬编码的像素值。
- **容器**:内容以 `max-w-content`(80rem / 1280px)居中,两侧使用 `px-4 sm:px-6` 边距。
- **顶栏**:粘性顶栏在 `sm`(640px)以下收起为一个汉堡菜单。
- **后台侧栏**:在 `md+` 固定为 180px,在 `md`(768px)以下折叠为下拉式面板——即 README 中所述的"在 ≤768px 自动折叠"。

---

## 8. 页面结构

### 顾客端

单一居中栏(`<main className="mx-auto max-w-content px-4 py-8 sm:px-6">`),搭配全局粘性 `HeaderNav`(logo、导航链接、购物车徽标、用户菜单、移动端汉堡菜单)。没有常驻侧栏;像*商品*和*结账*这类页面会按需自定义本地的多栏网格。Footer 在所有页面中固定存在。

### 后台端

`/admin` 拥有嵌套的 `layout.tsx`,在内容区旁提供侧栏(Dashboard、Products、Orders、Pricing、Analytics,以及供系统管理员使用的 System)。客户端的角色门禁(role gate)会对非管理员隐藏全部内容;真正的授权由后端的 `[Authorize(Roles)]` 与 Edge middleware 强制执行。

---

## 9. PWA 界面

- **Manifest**(`/manifest.json`):`display: standalone`、`start_url: /`、主题色、图标 → 可安装。
- **Service Worker**(`/sw.js`):预缓存应用外壳(app shell),导航请求采用 network-first 策略并以 `/offline` 兜底,静态资源采用 cache-first,跳过 `/api/` 请求。
- **离线页**(`/offline`):在离线时导航失败时展示的友好重试提示。

---

## 10. 主题与品牌重塑

要为 Novacart 品牌重塑,只需在 `globals.css` 中编辑 accent token:

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

由于每个组件都引用 `var(--accent)`(通过 Tailwind 的 `accent` 颜色映射),整个 UI 无需修改任何组件即可完成品牌重塑。ECharts 仪表板(`SalesChart.tsx`)同样读取 `var(--accent)`,因此图表会跟随主题变化。
