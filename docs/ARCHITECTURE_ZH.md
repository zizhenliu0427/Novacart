# Novacart — 系统架构

> 英文版:[ARCHITECTURE.md](ARCHITECTURE.md)

> 系统架构、分层、数据流与设计决策。
> 与 [ER 图](../Database_ER_Diagram.md)、[数据库规范](database-standards.md)及[部署指南](deployment-guide.md)配套阅读。

---

## 1. 概览

Novacart 是一个全栈电商平台,基于 **MonolithFirst(单体优先)** 理念构建:一个分层良好的单一应用作为基础,扩展能力(微服务、消息队列、全文检索)推迟到后期流量增长时再引入(参见 [README §Planned Enhancements](../README.md#planned-enhancements))。

系统服务于三类用户角色 —— **Customer(顾客)**、**Administrator(管理员)** 和 **System Administrator(系统管理员)** —— 覆盖商品浏览、购物车、结账、支付、订单管理、动态定价、数据分析以及 PWA 离线支持。

```
┌─────────────┐   HTTPS    ┌──────────────────┐   REST API    ┌────────────────────┐
│   Browser   │ ──────────▶│   Next.js 14      │ ────────────▶│  ASP.NET Core 8     │
│  (Client /  │ ◀──────────│   Frontend        │ ◀────────────│  Backend API        │
│   PWA)      │            │   (App Router)    │               │  (REST + JWT cookie)│
└─────────────┘            └──────────────────┘               └──────────┬───────────┘
                                                                            │
                                              ┌────────────────┬───────────┼───────────────┐
                                              ▼                ▼           ▼               ▼
                                        ┌──────────┐    ┌──────────┐  ┌──────────┐   ┌──────────┐
                                        │PostgreSQL│    │  Redis   │  │  Stripe  │   │  Square  │
                                        │ (source  │    │ (cache + │  │ (payment │   │ (catalogue│
                                        │  of truth│    │  session)│  │  webhook)│   │   sync)  │
                                        └──────────┘    └──────────┘  └──────────┘   └──────────┘
```

### 技术栈一览

| 层 | 技术 |
|---|---|
| 前端 | Next.js 14(App Router)、TypeScript、Tailwind CSS、ECharts |
| 后端 | ASP.NET Core 8(C#)、RESTful API |
| ORM | Entity Framework Core(Code-First,Npgsql provider) |
| 数据库 | PostgreSQL 16(唯一可信源 source of truth) |
| 缓存 | Redis 7(商品与订单采用 cache-aside 模式) |
| 支付 | Stripe(Checkout Sessions + 签名 webhook,令牌化 —— 不存储卡号) |
| 目录集成 | Square Catalogue API(sandbox 同步) |
| 认证 | JWT(HS256) access + refresh token 存于 HttpOnly cookie + bcrypt 密码哈希 |
| 邮件 | MailKit(SMTP),经有界 `Channel<T>` + `BackgroundService` worker 异步发送 |
| 对象存储 | S3 兼容(`IS3StorageService`);开发环境 **LocalStack**,生产环境 AWS S3 |
| 容器 | Docker、Docker Compose |
| 云目标 | AWS(EC2 / RDS / ElastiCache / S3) —— 见[部署指南](deployment-guide.md) |

---

## 2. 后端分层

后端遵循严格的 **`Controller → Service → Mapper → Entity`** 分层。每一层职责单一,通过约定加以约束:

```
┌─────────────────────────────────────────────────────────────┐
│  Controllers (thin)                                         │
│  Parse request · call a service · return DTO                │
│  No business logic · no EF queries · no try/catch           │
└──────────────────────┬──────────────────────────────────────┘
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  Factories & Strategies                                     │
│  OrderFactory (cart → order aggregate)                      │
│  PaymentStrategyFactory (resolve gateway by code)           │
│  IPaymentStrategy (StripePaymentStrategy)                   │
└──────────────────────┬──────────────────────────────────────┘
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  Services (all business logic + EF access)                  │
│  Interface + impl, registered in DI                         │
└──────────────────────┬──────────────────────────────────────┘
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  Mappers (static, entity → DTO projection)                  │
│  ProductMapper · OrderMapper                                │
└──────────────────────┬──────────────────────────────────────┘
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  Entities + AppDbContext (EF Core model)                    │
│  Never returned directly from controllers                   │
└─────────────────────────────────────────────────────────────┘
```

**分层规则(既定约定):**

- **Controllers** 很薄:解析请求、调用 service、返回 DTO。其中**不含业务逻辑、不含 EF 查询、不含 `try/catch`**。未处理异常由全局异常处理器捕获并映射为 `ProblemDetails`。
- **Services** 承载全部业务逻辑与 EF Core 访问。每个都是"接口 + 实现"成对出现(例如 `IAuthService` / `AuthService`),在 DI(依赖注入)容器中注册。
- **Mappers** 是将实体投影为 DTO 的静态类(`ProductMapper`、`OrderMapper`),完成 `Controller → Service → Mapper → Entity` 链路。
- **DTO** 位于 `backend/Models/Dtos/**`。EF 实体**绝不**直接从 controller 返回。

### Service 目录

| Service | 职责 |
|---|---|
| `AuthService` | 注册 / 登录 / 刷新;bcrypt 哈希;签发 JWT |
| `JwtTokenService` | HS256 access token 签名(Singleton,无状态) |
| `RefreshTokenService` | 不透明 refresh token 的生成 / 轮换 / 重用检测 / 全部吊销 |
| `ProductService` | 目录列表 / 详情;搜索、分面、排序、分页;Redis 缓存;动态定价 |
| `AdminProductService` | 商品 CRUD;slug 校验;元数据校验;软删除;缓存失效 |
| `CartService` | 已登录 + 游客购物车 CRUD;登录时合并;应用动态定价 |
| `OrderService` | 用户订单历史 / 详情(只读);Redis 缓存;所属权校验 |
| `PaymentService` | 结账编排;Stripe webhook 处理(幂等、事务化) |
| `AdminOrderService` | 管理员订单视图 + 6 态状态机 + 审计历史;状态邮件入队发送 |
| `PricingService` | 纯规则求值引擎(百分比 / 固定 / 定额,作用域优先级,时间窗口) |
| `PriceRuleService` | 定价规则 CRUD + 校验 |
| `UserService` | 顾客资料读取 / 更新 |
| `WishlistService` | 心愿单增删查(幂等,唯一约束去重) |
| `AddressService` | 收货 / 账单地址 CRUD;默认地址唯一性强制 |
| `AnalyticsService` | 销售聚合(总额、销售趋势、畅销品、低库存) |
| `SquareCatalogueService` | 从 Square API 同步目录(sandbox,带模拟回退) |
| `EmailService` | MailKit SMTP 发送(未配置时回退为控制台日志) |
| `EmailQueue` | 有界进程内队列;生产者入队,worker 发送 |
| `EmailBackgroundWorker` | 托管服务,消费 `EmailQueue` → 作用域内 `EmailService` |
| `S3StorageService` | 管理员商品图片上传的 presigned PUT/GET URL |
| `RedisCacheService` | 通用 Get/Set/Remove/RemoveByPrefix(JSON 序列化) |
| `GlobalExceptionHandler` | 将 `AppException` / `AuthException` 映射为 `ProblemDetails` |

### 设计模式

| 模式 | 位置 | 目的 |
|---|---|---|
| **Strategy(策略)** | `IPaymentStrategy` → `StripePaymentStrategy`;`PaymentStrategyFactory` | 可替换的支付网关。新增服务商 = 实现该接口 + 在 DI 中注册。 |
| **Factory(工厂)** | `OrderFactory.CreateFromCart(...)` | 将订单聚合(订单 + 明细 + 价格快照 + 地址快照)的构造与 `PaymentService` 隔离。 |
| **Gateway(网关)** | `ISquareCatalogueGateway` 包装 Square SDK | 让第三方客户端在测试中可被 mock。 |
| **Cache-aside(旁路缓存)** | `IRedisCacheService` 被 `ProductService` / `OrderService` 使用 | 读穿透缓存,写入时按前缀失效。 |
| **全局异常处理器** | `IExceptionHandler`(`GlobalExceptionHandler`) | 单一入口将领域异常映射为 HTTP `ProblemDetails`;controller 保持干净。 |

---

## 3. 错误处理

领域异常携带 HTTP 状态码,由全局处理器统一翻译 —— **controller 中绝不出现 `try/catch`**。

```
Service throws                          GlobalExceptionHandler            HTTP response
─────────────────                       ──────────────────────           ──────────────
AppException(message, statusCode)  ───▶ maps to ProblemDetails      ───▶ { status, title, detail, instance }
AppException.NotFound()            ───▶ 404
AppException.Conflict()            ───▶ 409
AppException.Forbidden()           ───▶ 403
AuthException(message, statusCode) ───▶ its status (401/403)
UnauthorizedAccessException        ───▶ 401   (thrown by GetUserId() helpers)
(any other)                        ───▶ 500   (logged at Error level)
```

模型校验失败(如 `[Required]`、`[Url]`)由 `ConfigureApiBehaviorOptions` 整形为 RFC 7807 规范的 `ValidationProblemDetails`(400),并附带 `errors` 字典。

---

## 4. 横切基础设施

### DI 注册(`Program.cs`)

- **Singleton(单例)**(无状态 / 线程安全):`IConnectionMultiplexer`(Redis)、`IRedisCacheService`、`IJwtTokenService`、`EmailQueue`、`IS3StorageService`。
- **Scoped(每请求)**: `AppDbContext`、全部业务 service、工厂、策略(含 `IRefreshTokenService`)。
- **Hosted services(托管服务)**: `EmailBackgroundWorker` 在后台线程消费邮件队列。

### 中间件管道(顺序很重要)

```
UseExceptionHandler()          ← first; catches everything downstream
  └─ UseResponseCompression() / UseResponseCaching()   ← Brotli/Gzip + HTTP cache headers
      └─ UseSwagger() / UseSwaggerUI()                 ← Dev only
          └─ UseCors("AllowFrontend")                  ← credentials allowed for cookie auth
              └─ UseAuthentication()                   ← JWT bearer (reads HttpOnly cookie)
                  └─ UseAuthorization()                ← role-based [Authorize]
                      └─ MapControllers() + /api/health
```

### 安全

- **Access JWT 存于 HttpOnly cookie**(`novacart_jwt`,15 分钟,`Secure`、`SameSite=Strict`、`Path=/api`):前端永远接触不到原始 token —— 可防范 XSS。bearer 中间件通过 `OnMessageReceived` 从 cookie 读取,并为 Swagger 保留 Bearer 头回退。
- **Refresh token 存于 HttpOnly cookie**(`novacart_refresh`,7 天,`Path=/api/auth`):作用域更窄 —— 仅发往 auth 端点。每次 `POST /api/auth/refresh` 轮换;若已吊销 token 被再次使用,则吊销该用户全部会话。
- **边缘标记 cookie**(`novacart_authed=1`):一个非敏感标记,Next.js Edge 中间件读取它来守卫受保护路由(因为 Edge 无法从 `localStorage` 访问 HttpOnly JWT)。
- **RBAC(基于角色的访问控制)**:3 种角色(`customer` / `admin` / `sysadmin`)作为 JWT claim 携带。管理员端点使用 `[Authorize(Roles = RoleNames.AdminRoles)]`(`admin,sysadmin`);敏感系统操作使用 `[Authorize(Roles = RoleNames.SysAdmin)]`(仅 sysadmin)。
- **支付令牌化**:卡数据由 Stripe 处理;Novacart 从不存储卡号 —— 只存 Stripe 的 payment intent / session ID。
- **密码哈希**:bcrypt(加盐、单向)。

---

## 5. 数据流 —— 结账与支付(端到端)

这是最关键的流程;它用到了 Strategy + Factory 模式、幂等性以及事务一致性。

### A. 创建结账会话

```
Browser                Next.js                 CheckoutController            PaymentService
  │  click "checkout"    │  POST /api/checkout    │  CreateCheckout()           │  ProcessCheckoutAsync()
  │ ────────────────────▶│ ─────────────────────▶│ ──────────────────────────▶│
  │                      │                        │                            │  1. load cart (409 if empty)
  │                      │                        │                            │  2. validate stock (410/422)
  │                      │                        │                            │  3. load active price rules
  │                      │                        │                            │  4. OrderFactory.CreateFromCart()
  │                      │                        │                            │       → subtotal/shipping/tax/total
  │                      │                        │                            │       → OrderItem snapshots price
  │                      │                        │                            │  5. save Order (Pending)
  │                      │                        │                            │  6. PaymentStrategyFactory → Stripe
  │                      │                        │                            │  7. StripePaymentStrategy.CreateSession
  │                      │                        │                            │  8. record Payment (Pending)
  │                      │                        │  ← CheckoutResponse{url}   │
  │                      │  redirect to Stripe    │ ◀──────────────────────────│
  │ ◀────────────────────│ ◀──────────────────────│                             │
```

### B. Stripe webhook(支付确认)

```
Stripe                 CheckoutController          PaymentService.HandleWebhookAsync
  │  checkout.session.completed                       │
  │  (signed)                                         │
  │ ────────────────────────────────────────────────▶│
  │                                                   │  1. verify signature (400 if invalid)
  │                                                   │  2. idempotency: insert PaymentWebhook
  │                                                   │     (unique idx on event_id → 200 + return on dup)
  │                                                   │  3. ExecutePaymentCompletionAsync (DB TRANSACTION):
  │                                                   │       - reload order (skip if not Pending)
  │                                                   │       - re-check stock
  │                                                   │       - decrement stock
  │                                                   │       - Order → Paid, Payment → Succeeded
  │                                                   │       - clear user's cart
  │                                                   │     COMMIT
  │                                                   │  4. enqueue confirmation email (EmailQueue → worker)
  │                                                   │  5. invalidate order + product caches
  │  ◀──────── 200 OK ──────────────────────────────│
```

**关键保证:**
- **幂等性**:唯一索引 `idx_payment_webhooks_event_id` 意味着重放的 webhook 是空操作。
- **原子性**:库存扣减 + 状态流转 + 购物车清空发生在同一个数据库事务中 —— 要么全部成功,要么全部回滚。
- **价格冻结**:`OrderItem.PriceAtPurchase` 在结账时即快照;后续定价规则变更不会影响历史订单。

---

## 6. 缓存策略

Redis 已接入并实际使用(cache-aside 模式),支持按前缀失效。

| 对象 | Key 模式 | TTL | 失效来源 |
|---|---|---|---|
| 商品列表 | `products:list:{filters}:p{page}` | 60s | `AdminProductService` 写入;`PaymentService` webhook |
| 订单列表(按用户) | `orders:user:{userId}:p{page}` | 30s | 新订单 / webhook |
| 订单详情 | `orders:detail:{orderId}` | 30s | (TTL 到期 —— 见下方已知问题) |

订单详情命中缓存后,返回前仍会执行**所属权校验**,防止跨用户数据泄露。

---

## 7. 前端架构

Next.js 14 **App Router**,采用基于 Context 的状态模型。

```
RootLayout
 ├─ AuthProvider          (login/register/logout, /me rehydration)
 │   └─ CartProvider      (Loads on auth, guest cart by session)
 │       └─ WishlistProvider (hydrates on auth, optimistic toggle)
 │           └─ ToastProvider (global notifications)
 │               ├─ HeaderNav      (sticky top bar, cart badge, user menu)
 │               └─ <main>         (page content)
 ├─ footer
 └─ <script> register /sw.js   (PWA service worker)
```

- **路由守卫**:Next.js Edge `middleware.ts` 检查 `novacart_authed` 标记 cookie,以保护 `/cart`、`/orders`、`/checkout`、`/admin/*`,并在需要时重定向。
- **Admin 外壳**:`/admin` 拥有自己的嵌套 `layout.tsx`,带侧边栏(在 `md`/768px 以下折叠为汉堡菜单)和客户端角色门禁。
- **API 层**:`apiCall` 封装(`lib/api.ts`)发送 `credentials: 'include'`,401 时自动刷新(合并的 `POST /api/auth/refresh` 后重试一次),解析 `ProblemDetails` / 校验错误,并区分刷新失败(重定向登录)与 403(权限错误,停留原处)。
- **设计系统**:基于设计 token(见 [UI Design](UI-DESIGN.md));ECharts 仪表板动态导入(`ssr:false`)以兼容 App Router。

---

## 8. 已知技术债务(非阻塞)

此处如实记录,留待未来重构 —— 它们不影响当前功能:

1. **定价规则加载存在重复**:`PaymentService`、`ProductService` 和 `CartService` 各有自己的 `LoadActiveRulesAsync`。应抽取为共享 helper。
2. **索引命名不一致**:`OnModelCreating` 使用 `idx_table_col`(阿里风格),但 `AddPerformanceIndexes` 迁移保留了 EF 默认值(`IX_Table_Col`)。应统一为一种约定。
3. **订单详情缓存未在 webhook 完成时失效**(只清除了列表前缀);详情行依赖 30s TTL 到期。存在轻微的过期窗口。
4. **`AnalyticsService` 未注入缓存**,但 `AdminSystemController.ClearCache` 已经会删除 `analytics:` 前缀 —— 失效钩子已预接好却未启用。
5. **部分 controller 的 XML 注释已过时**(`UsersController` / `WishlistController` 写着 "SCAFFOLD / 501",但 service 已完全实现)。

---

## 10. 未来微服务目标架构（PE-1 — 进行中）

当 [HANDOFF §11](../HANDOFF.md#11-planned-enhancements-scaling-tail--not-scheduled) 中的扩展触发条件满足时，Novacart 采用以下**已批准最终方案**（取代早期 Consul/Ocelot 简述）：

| Concern | 最终选型 | 状态 (2026-07-16) |
|---------|----------|-------------------|
| 编排 / 本地开发 | **.NET Aspire** AppHost | Docker Compose 默认栈 |
| API 网关 | **YARP** | 已上线 |
| 服务发现 | Aspire → **Kubernetes**（不用 Consul） | 仅 Compose DNS |
| 同步容错 | **Polly** + Refit | ServiceDefaults 内 Polly |
| 消息 | **RabbitMQ** + **MassTransit** | 已上线 |
| 可靠发布 | **Transactional Outbox**（MassTransit EF，Order 库） | 已上线 |
| 结账编排 | **MassTransit Saga** | **已上线** — `OrderCheckoutStateMachine` |
| 多实例库存 | **Redis Redlock**（PE-4，同阶段） | **已上线** — `StockReservationService` |

服务：**Auth**、**Product**、**Cart**、**Order** — 各库独立（目标）；Phase 1–3 仍**共享 PostgreSQL**。**PE-2** 与 **PE-5** 已**并入本方案**。

### 10.1 Redis 库存锁（PE-4）

Product 服务 **`StockReservationService`** 在结账路径（`PaymentCompleted` → `ReserveStockConsumer`）对每个 `ProductId` 加 Redis 锁：键 `novacart:stock:lock:{productId:N}`，TTL 30s，`SET NX` + Lua 释放；多 SKU 按键名字典序加锁；抢锁失败由 MassTransit 重试。

完整拓扑：[docs/MICROSERVICES-PE1_ZH.md](MICROSERVICES-PE1_ZH.md) · [English](MICROSERVICES-PE1.md)。

---

## 11. 相关文档

- [数据库 ER 图与 Schema 设计](../Database_ER_Diagram.md) —— 实体关系、架构层面的 Schema 决策、索引策略
- [数据库规范](database-standards.md) —— 阿里规范审计、Guid 主键理由
- [UI 设计系统](UI-DESIGN.md) —— 设计 token、组件库、响应式策略
- [部署指南](deployment-guide.md) —— AWS 架构、生产环境 Docker Compose、环境变量参考
- [Stripe Webhook 本地测试](STRIPE_WEBHOOK_LOCAL.md) —— ngrok + Stripe CLI 配置
- [PE-1 微服务最终方案（未来）](MICROSERVICES-PE1_ZH.md) —— Aspire + YARP + MassTransit + RabbitMQ + Saga + Outbox
- [P14 项目规格说明](../P14_Modern_Ecommerce_Web_App.md) —— 本系统所满足的原始需求
