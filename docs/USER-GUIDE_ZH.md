# Novacart — 用户指南

> 英文版:[USER-GUIDE.md](USER-GUIDE.md)
> 本指南面向三类用户:顾客、管理员和系统管理员。
> 部署与初始化说明请参阅 [README Getting Started](../README.md#getting-started)。

---

## 启动应用

用 Docker 一键启动所有服务:

```bash
docker compose up --build -d
```

启动后访问以下地址:

| 名称 | URL |
|---|---|
| 商城前台(Storefront) | http://localhost:3001 *(本地重映射后的端口;生产环境 compose 中为 3000)* |
| API / Swagger 文档 | http://localhost:5000/swagger |
| 健康检查 | http://localhost:5000/api/health |

---

## 1. 顾客指南

### 注册与登录

1. 点击顶栏的 **Sign in**(登录)→ **Register**(注册)。
2. 填写姓名、邮箱和密码。注册成功后会自动登录。
3. 老用户:用邮箱 + 密码点击 **Sign in** 登录。

你的登录会话保存在 **HttpOnly cookie** 中,在 token 过期或你主动登出之前,即使重启浏览器也能保持登录状态。登出操作会清除该 cookie。

### 浏览与搜索商品

- **Products**(商品)页:支持关键词搜索(带防抖)、多选分类筛选、价格区间、标签筛选,以及排序(最新 / 价格 / 名称)。
- 点击商品卡片可查看完整详情,其中包含一张根据商品 `metadata` 自动生成的 **动态规格表**(不同商品类型的属性不同——电子产品显示接口/电池,服装显示尺码/面料等)。
- 商品图片默认取自 Unsplash URL;管理员可以从 Square 同步,也可以自行上传。

### 购物车

- 在商品卡片或详情页点击 **Add**(加入购物车)。
- 顶栏的购物车角标会显示商品数量。
- 在 **Cart**(购物车)页:可调整数量(步进器,上限为库存)、删除商品,并查看实时小计金额(已应用动态定价)。
- **游客购物车**:登录前也可以先把商品加入购物车——登录后会自动合并到你账户的购物车里。

### 结算与支付

1. 在购物车页点击 **Proceed to checkout**(去结算)。
2. 选择一个已保存的收货地址(也可在 **Account**(账户)页统一管理地址)。
3. 你会被跳转到 **Stripe Checkout**(Stripe 收银台,沙箱环境)。使用测试卡完成支付:
   - 卡号:`4242 4242 4242 4242`
   - 有效期、CVC、邮编可任意填写(有效期需为未来日期)。
4. 支付成功后会进入确认页;取消支付则会跳转到取消页。

> 不会产生真实扣款——这是 Stripe 的沙箱环境。Novacart 永远不会看到或存储你的卡号(通过 tokenisation 令牌化处理)。

### 订单历史

- **Orders**(订单)页列出历史购买记录,并带有状态标签(Pending、Paid、Processing、Shipped、Completed、Cancelled)。
- 展开某条订单可查看其中的商品条目,价格均为 **冻结价格**(即你下单时实际支付的价格,不受后续价格调整影响)。

### 个人资料、心愿单与地址

- **Account**(账户):可编辑你的姓名(邮箱为只读,不可修改)。
- **Wishlist**(心愿单):在商品卡片或详情页点爱心即可收藏;在 Wishlist 页可管理已收藏的商品。
- **Addresses**(地址):可新增/编辑/删除收货与账单地址,并可设置默认地址(同一时间只能有一个默认收货地址 + 一个默认账单地址)。

### 安装为应用(PWA)

商城前台是一个 **Progressive Web App**(渐进式 Web 应用)——在 Chrome/Edge 中,可使用地址栏中的安装按钮将其添加为独立应用。对于访问过的页面,它支持离线访问(网络断开时会显示离线页面)。

---

## 2. 管理员指南

> 管理相关接口需要 `admin` 或 `sysadmin` 角色。在 Development(开发)环境下,系统启动时会自动创建一个管理员账号。

### 开发环境管理员引导账号(仅限 Development)

配置位于 `backend/appsettings.Development.json`:

```
Email:    admin@novacart.local
Password: Admin123!
```

用这套凭据在前台登录后,你会在用户菜单里看到一个 **Admin dashboard**(管理后台)入口。

### 商品管理(`/admin/products`)

- **列表**:显示所有商品(上架 + 下架),支持搜索、状态筛选和分页。
- **创建 / 编辑**:可填写名称、slug、价格、币种、库存数量、分类、metadata(JSON)和图片 URL。
- **库存**:可调整库存;低库存和无库存标签会自动显示。
- **Deactivate**(停用,即软删除):从前台下架商品但不会删除其数据;之后可以重新上架。
- **Sync from Square**(从 Square 同步):从 Square 沙箱拉取商品目录(未设置 token 时则使用一组模拟数据)。

### 订单管理(`/admin/orders`)

- 查看所有订单,可按订单号 / 顾客邮箱搜索,并按状态筛选。
- 打开某条订单可查看其中的商品条目和总额。
- **推进状态**:在经过校验的状态机中流转:
  `pending → paid → processing → shipped → completed`
  处于 `pending` 或 `paid` 状态时允许取消。
- 非法的状态流转会被拒绝。每次状态变更都会记入审计历史(含操作人和时间戳),并向顾客发送一封状态更新邮件。

### 定价规则(`/admin/pricing`)

- 创建规则:范围(单商品 / 分类 / 全局)、类型(百分比折扣 / 固定金额减免 / 固定价格)、数值、生效时间窗口以及启用开关。
- 规则在读取价格时统一生效,覆盖商品目录、购物车和结算环节。**最具体的规则优先**(商品 > 分类 > 全局)。
- 历史订单价格不受影响(已按下单时价格冻结)。

### 数据分析(`/admin/analytics`)

- KPI:总营收、总订单数、售出件数、平均订单金额。
- **Sales over time**(销售趋势)图表(基于 ECharts)——营收(折线)与订单数(柱状),并自动补齐无数据的日期空缺。
- **Best sellers**(畅销商品)和 **low-stock**(低库存)表格。

---

## 3. 系统管理员指南

> `sysadmin` 角色拥有管理员的一切权限,**另外**还能执行系统级操作。

### 系统诊断(`/admin/system`)

仅限 `sysadmin` 访问(`[Authorize(Roles = RoleNames.SysAdmin)]`):

- **健康仪表盘**:深度数据库连通性检查 + Redis ping 状态。
- **Clear cache**(清除缓存):清空 Redis 中商品 / 订单 / 分析数据相关的缓存前缀。在批量数据变更后或怀疑数据陈旧时使用。

> 角色区分如下:`admin` 管理 *业务* 数据(商品、订单、定价);`sysadmin` 管理 *系统*(缓存、诊断)。这就是 P14 阶段引入的三角色划分。

---

## 4. 故障排查

| 症状 | 可能原因 / 处理办法 |
|---|---|
| 无法登录 / 出现 401 循环 | Cookie 被浏览器拦截;确保站点通过 `http://localhost`(而不是 IP)访问,以便 `SameSite=Strict` 生效。 |
| Stripe 结算失败 | 沙箱密钥配置有误;检查 `.env` 中的 `STRIPE_SECRET_KEY`。Webhook 没触发?参见 [Stripe webhook local testing](STRIPE_WEBHOOK_LOCAL.md)。 |
| 看不到管理后台入口 | 你的账号不是 `admin`/`sysadmin`。请使用开发环境引导账号,或在数据库中为账号赋予相应角色。 |
| 无法从 Square 同步商品 | 未设置访问令牌;系统会回退到模拟同步。请在 `.env` 中设置 `SQUARE_ACCESS_TOKEN`。 |
| 管理员改动后数据仍是旧的 | 缓存 TTL 为 60 秒(商品)/ 30 秒(订单)。sysadmin 可通过 `/admin/system` 立即清空缓存。 |

---

## 相关文档

- [架构设计](ARCHITECTURE.md) · [UI 设计](UI-DESIGN.md) · [部署指南](deployment-guide.md)
