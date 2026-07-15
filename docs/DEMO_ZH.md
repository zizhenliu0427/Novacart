# Novacart — 演示指南

> 英文版:[DEMO.md](DEMO.md)

> 这是一份用来照着念的演示脚本。覆盖了完整的端到端"正常路径",再加上后台、分析、PWA 这几个亮点环节。整体时长大约 10–12 分钟。

---

## 开讲之前

1. **把整套服务拉起来**(建议先清干净):
   ```bash
   docker compose down -v        # 清掉旧数据
   docker compose up --build -d  # 重新构建 + 启动
   ```
2. 等后端跑完迁移和种子数据(盯着 `docker compose logs -f backend`,看到启动完成就行)。
3. 打开前台店铺首页:**http://localhost:3001**。
4. 再开一个标签页打开 Swagger:**http://localhost:5000/swagger**(展示 API 全貌很方便)。
5. 把这些账号准备好:

| 角色 | 邮箱 | 密码 |
|---|---|---|
| Admin / SysAdmin(开发环境) | `admin@novacart.local` | `Admin123!` |
| 顾客(现注册一个) | *(演示时现场创建)* | *(自己定)* |

6. Stripe 测试卡号:**`4242 4242 4242 4242`**,有效期随便填个未来的、CVC 和邮编随意。

---

## 演示脚本

### 第一幕 — 顾客购物全流程(5 分钟)

1. **注册一个新顾客。** 展示自动登录,购物车角标和用户菜单都出来了。顺嘴提一下 **HttpOnly cookie**(JWT 根本不碰 JS → 天然防 XSS)。

2. **逛商品目录。**
   - 展示响应式网格(拖动窗口大小:4 → 3 → 2 → 1 列)。
   - 演示**多条件筛选搜索**:输个关键词,勾上几个分类,设个价格区间,选个标签 chip,再换换排序方式。
   - 点开一个**商品详情** —— 指出那张*动态规格表*(由元数据驱动,不同类型的商品字段不一样)。

3. **加购物车 + 收藏。**
   - 加 2–3 个商品;购物车角标实时跳动。
   - 点个心收藏 → 去 Wishlist(收藏夹)页面看它出现了。

4. **用 Stripe 结账。**
   - 进购物车 → 点 **Proceed to checkout**(去结算)→ 选个收货地址(没有就在 Account 里现加一个)。
   - 跳转到 Stripe 沙箱 → 用 `4242…` 支付。重点强调:**卡数据根本不经过我们服务器**(令牌化)。
   - 落到**支付成功页**。

5. **订单历史。** 展示刚付完的那单,展开它 —— 重点指出**冻结价格**(下单那一刻的快照)。

6. **PWA(可选)。** 展示地址栏里的安装图标;装上;演示离线(DevTools → Network → Offline → 刷新 → 离线页)。

> **可聊的谈资:** 动态定价、Redis 缓存(商品/订单读取很快)、幂等 webhook(重试很安全)。

### 第二幕 — 后台能力(4 分钟)

用 `admin@novacart.local` 登录。

1. **Admin dashboard(后台控制台)** —— 展示侧边栏导航(768px 以下会折叠成汉堡菜单)。

2. **商品管理**(`/admin/products`):
   - 按状态筛选、搜索、翻页。
   - **编辑一个商品**:改个价格或库存;展示低库存角标。
   - **创建一条定价规则** → 进 `/admin/pricing`,给某个分类加一条打八折的规则 → 回到前台,展示更新后的价格 + 划掉的对比价。

3. **订单管理**(`/admin/orders`):
   - 找到第一幕里的那单。
   - **推进它的状态**:paid → processing → shipped。每走一步都会给顾客发邮件,并写一条审计历史记录。

4. **数据分析**(`/admin/analytics`):
   - 展示 KPI 卡片(营收、订单数、客单价 AOV)。
   - 那张**销售趋势** ECharts 图(跟随主题 —— 切换系统的深色模式,展示它跟着变)。

> **可聊的谈资:** RBAC(三种角色)、经过校验的订单状态机、用设计 token 做的 ECharts 主题化。

### 第三幕 — 系统管理 + 工程深度(2 分钟)

1. **系统诊断**(`/admin/system`)—— 只有 `sysadmin` 能看到。展示数据库/Redis 健康探针,以及那个**清缓存**操作。

2. **Swagger**(`/swagger`)—— 展示 API 全貌、Bearer 认证按钮,以及每个端点都标注了它的响应类型(`[ProducesResponseType]`)。

3. **(可选)现场跑测试:**
   ```bash
   docker build -f Dockerfile.backend.test -t novacart-backend-test . && docker run --rm novacart-backend-test
   docker build -f Dockerfile.frontend.test -t novacart-frontend-test . && docker run --rm novacart-frontend-test
   ```

> **可聊的谈资:** 分层架构(Controller → Service → Mapper → Entity)、Strategy(支付)+ Factory(订单)、CI/CD 流水线、全局异常处理。

---

## 截图清单

为了做静态作品集 / README,把这些画面截下来:

- [ ] 前台首页(主视觉 + 后端状态卡片)
- [ ] 商品页带激活的筛选(分类 chip + 价格 + 搜索)
- [ ] 商品详情页带动态规格表
- [ ] 购物车页(步进器 + 订单摘要 + 低库存提醒)
- [ ] Stripe Checkout 跳转页
- [ ] 订单历史(展开的卡片,带冻结价格)
- [ ] 后台商品表格(状态徽章、分页)
- [ ] 后台定价规则表单 + 前台的对比价
- [ ] 后台订单详情 + 状态流转按钮
- [ ] 分析仪表盘(KPI + ECharts 图,亮色 + 暗色)
- [ ] 系统诊断(sysadmin 视图)
- [ ] PWA 安装提示 / 独立窗口
- [ ] Swagger UI

---

## 测试数据参考

- **种子商品目录**:5 个分类(Electronics、Apparel、Home & Living、Accessories、Books)共 12 件商品,每件都带 `metadata` + Unsplash 图片。
- **种子角色**:`customer`(1)、`admin`(2)、`sysadmin`(3)。
- **开发环境管理员**:`admin@novacart.local` / `Admin123!`(开发环境下自动种入)。
- **Stripe 测试卡号**:`4242 4242 4242 4242`。

---

## 相关文档

- [用户指南](USER-GUIDE.md) · [架构](ARCHITECTURE.md) · [UI 设计](UI-DESIGN.md)
