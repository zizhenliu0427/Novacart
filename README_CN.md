# Novacart — 现代电商 Web 应用

> English version: [README.md](README.md)

一个现代化的电商平台，采用 .NET 后端 + Next.js 前端构建。**默认部署为微服务**（YARP 网关 + Auth/Product/Cart/Order + RabbitMQ + MassTransit Saga）。遗留单体：`docker-compose.monolith.yml`。

---

## 目录

- [项目概述](#项目概述)
- [优先级 1 — MVP 核心功能](#优先级-1--mvp-核心功能)
- [优先级 2 — P14 项目要求](#优先级-2--p14-项目要求)
- [优先级 3 — 技术增强](#优先级-3--技术增强)
- [后续增强规划](#后续增强规划)
- [技术栈](#技术栈)
- [系统架构](#系统架构)
- [快速开始](#快速开始)
- [项目结构](#项目结构)
- [API 参考](#api-参考)
- [部署方案](#部署方案)
- [开源协议](#开源协议)

---

## 项目概述

Novacart 是一个全栈电商 Web 应用。MVP 实现五个核心功能：用户认证、商品浏览、购物车、Stripe 结账和订单历史。基于 .NET、Next.js、PostgreSQL、Redis 和 Docker 构建，平台设计支持未来扩展。

---

## 优先级 1 — MVP 核心功能

> 这些是**必须完成**的功能，没有它们项目就不完整。

### 1. 用户注册与登录

- 基于 **JWT（JSON Web Token）** 的注册和登录
- 密码使用 **bcrypt** 哈希（加盐、单向）
- 会话管理：短效 access JWT + 轮换 refresh token 存储在 **HttpOnly Cookie** 中（防止 XSS）
- 路由保护 — 未登录用户重定向到登录页

### 2. 商品浏览

- 通过 **PostgreSQL 种子数据** 展示商品目录（5 个分类、12 个商品）—— **Square Catalogue API** 集成在 P2 实现
- 展示商品名称、描述、图片和价格
- 按关键词和分类搜索、筛选商品

### 3. 购物车

- 添加、删除、更新商品数量
- 实时价格计算
- 通过 **React Context API** + 可复用组件（CartItem、CartSummary）管理购物车状态
- 登录用户的购物车跨会话持久化

### 4. 结账与支付

- 支付前订单摘要预览
- **Stripe 沙盒**支付处理（测试模式，无真实扣款）
- **Stripe Webhooks** 支付确认
- 开发环境使用 **ngrok** 暴露本地 Webhook 端点
- 服务端不存储银行卡信息（令牌化）

### 5. 订单历史

- 查看所有历史订单的完整详情
- 订单存储在 **PostgreSQL**（持久化）
- **Redis** 缓存加速近期订单检索
- 订单详情：时间戳、订单 ID、商品列表、价格、支付状态

---

## 优先级 2 — P14 项目要求

> 来自 UNSW COMP9900 P14 项目规范的功能。MVP 完成后再实现。

### 6. PWA 与响应式设计

- **渐进式 Web 应用**：Web App Manifest、Service Worker、可安装、独立模式
- **移动优先响应式布局**：
  - CSS Grid 自适应断点：4 → 3 → 2 → 1 列
  - rem 单位用于字号和间距（无硬编码像素）
  - CSS 自定义属性确保间距一致
  - 媒体查询处理设备适配

### 7. 管理后台（P14 要求）

- 商品管理（增删改查）
- 订单状态管理（`pending → paid → processing → shipped → completed → cancelled`）
- 销售分析仪表盘（ECharts）：总销售额、每日订单、收入汇总

### 8. 基于角色的访问控制（P14 要求）

- 三种角色：**顾客**、**管理员**、**系统管理员**
- 角色声明嵌入 JWT Token
- 管理员专属端点拒绝非管理员 Token（403 Forbidden）

### 9. 高级搜索与筛选（P14 要求）

- 多分类搜索，支持类型筛选和排序
- 商品关键词搜索

---

## 优先级 3 — 技术增强

> 展示工程最佳实践和技术深度，有时间再加。

### 10. 前端架构

- **可复用组件**：Sidebar、Header、ProductCard、CartItem（减少约 40% 重复代码）
- **Custom React Hooks**：`useAuth`、`useCart`（通过 Context Provider）
- **响应式侧边栏**：≤768px 自动折叠
- **嵌套路由 + 路由守卫**：Next.js App Router + Middleware

### 11. API 与文档

- **统一 API 层**：`apiCall` 封装，Cookie 认证（`credentials: 'include'`）、401 自动刷新、错误解析、401/403 处理
- **Swagger / OpenAPI**：自动生成交互式 API 文档（Swashbuckle）

### 12. 测试与 CI/CD

- **后端**：xUnit 单元测试 + 集成测试
- **前端**：Vitest + React Testing Library
- **CI/CD**：GitHub Actions（自动构建、自动测试、部署）

### 13. 代码质量

- **分层架构**：Controller → Service → Mapper → Entity
- **设计模式**：工厂模式（对象创建）、策略模式（支付提供商）
- **数据库规范**：阿里巴巴开发规范（命名、索引、SQL 指南）

---

## P14 Preferred 交付能力（已完成）

> P14 规范中若干 core 之外的 preferred 能力，现已实现：

| 能力 | 实现 |
|---|---|
| **JWT Refresh Token** | 15 分钟 access token + 7 天 refresh token（HttpOnly cookie、DB 持久化、轮换与重用检测）。见 [架构文档 §4](docs/ARCHITECTURE_ZH.md#4-横切基础设施)。 |
| **异步邮件队列** | MailKit SMTP + 有界 `Channel<T>` + `BackgroundService`，webhook/请求处理器入队后立即返回。见 [架构文档 §5](docs/ARCHITECTURE_ZH.md#5-数据流--checkout--payment-end-to-end)。 |
| **S3 对象存储** | `IS3StorageService` presigned URL；管理员直传商品图片。开发环境 **LocalStack**，生产 AWS（配置切换）。见 [部署指南](docs/deployment-guide.md)。 |

### P14 文档交付物

| 交付物 | 文档 |
|---|---|
| 架构与设计 | [docs/ARCHITECTURE_ZH.md](docs/ARCHITECTURE_ZH.md)（[EN](docs/ARCHITECTURE.md)） |
| UI 设计 | [docs/UI-DESIGN_ZH.md](docs/UI-DESIGN_ZH.md)（[EN](docs/UI-DESIGN.md)） |
| 数据库 Schema | [Database_ER_Diagram.md](Database_ER_Diagram.md) + [docs/database-standards.md](docs/database-standards.md) |
| 测试 API | [Swagger](http://localhost:5000/swagger) + Docker 测试镜像 |
| 部署 | [docs/deployment-guide.md](docs/deployment-guide.md) |
| CI/CD | [.github/workflows/ci.yml](.github/workflows/ci.yml) |
| 用户指南 | [docs/USER-GUIDE_ZH.md](docs/USER-GUIDE_ZH.md)（[EN](docs/USER-GUIDE.md)） |
| Demo 材料 | [docs/DEMO_ZH.md](docs/DEMO_ZH.md)（[EN](docs/DEMO.md)） |
| 技术笔记 | [TECH_NOTES_CN.md](TECH_NOTES_CN.md) |

---

## 后续增强规划

> 平台扩展时的未来迭代。**尚未实现** — 不属于 P14 或当前版本。
> 任务清单见 **[TODO.md](TODO.md)**，背景与触发条件见 [HANDOFF.md §11](HANDOFF.md#11-planned-enhancements-scaling-tail--not-scheduled)。

| 技术 | 用途 |
|---|---|
| **微服务（PE-1 最终方案）** | **.NET Aspire** + **YARP** + **Polly**；拆分为 Auth / Product / Cart / Order。**MassTransit + RabbitMQ**、**Transactional Outbox**、**Saga** 编排结账。**设计文档：** [docs/MICROSERVICES-PE1_ZH.md](docs/MICROSERVICES-PE1_ZH.md) |
| **RabbitMQ + 异步订单** | ✅ **已纳入 PE-1 最终方案** — MassTransit 持久化消息，替代规模化后的进程内 `EmailQueue` |
| **ElasticSearch** | ✅ **已实现（PE-3）** — Product API 关键词搜索；Postgres 回退 |
| **分布式锁与库存加固（PE-4）** | **基线 ✅：** 多实例扣库存 Redis 锁 + 并发压测。**后续：** 预占库存（PaymentIntent TTL）、DB 原子扣减、网关限流、Redis 高可用、锁/库存监控。**PE-6 Redis 购物车 ≠ 库存锁** |
| **异步订单处理** | ✅ **已纳入 PE-1 最终方案** — MassTransit Saga（支付 → 库存 → 邮件 → 清购物车） |
| **购物车优化（PE-6）** | ✅ Redis 快照（Postgres 真相源）；默认关闭 |
| **SQL 分表（PE-7）** | ✅ UserId hash 试点；默认关闭 |
| **线程池调优（PE-8）** | ✅ 可配置 min threads + 可选 webhook 热路径队列；默认关闭 — [docs/PE8-THREAD-POOL.md](docs/PE8-THREAD-POOL.md) |
| **AI 聊天机器人（PE-9）** | ✅ OpenAI / Claude / Ollama — [docs/PE9-AI-CHATBOT.md](docs/PE9-AI-CHATBOT.md) |
| **国际化（i18n）** | ✅ **完整** — `/en/` + `/zh/`；admin 表单 + hreflang — [docs/PE10-I18N.md](docs/PE10-I18N.md) |

### Spring Cloud 大型购物网站 — 库存与结账（对照）

生产级 Java/Spring Cloud 商城通常**叠加多层**（不单靠 Redis 锁）：

| 层次 | Spring 常见方案 | Novacart（.NET） |
|---|---|---|
| 网关 + 限流 | Spring Cloud Gateway + Sentinel | YARP — **限流待做（PE-4）** |
| 结账编排 | Spring Cloud Stream + Saga / Seata | ✅ MassTransit Saga + Outbox |
| 扣库存锁 | Redisson / Redis | ✅ `RedisDistributedLockService` |
| **预占库存** | Redis/DB 占位 + TTL | **规划（PE-4）** |
| DB 最后防线 | `UPDATE … WHERE stock >= qty` | **规划（PE-4）** |
| 秒杀队列 | MQ + 限流削峰 | **规划（PE-4）** |
| 购物车缓存 | Redis | 现 Postgres → **PE-6** |
| 搜索 | Elasticsearch | ✅ PE-3 |

详见 [TODO.md § PE-4](TODO.md#pe-4--distributed-lock--inventory-hardening-redis) · [MICROSERVICES-PE1_ZH.md](docs/MICROSERVICES-PE1_ZH.md)

---

## 技术栈

### MVP 核心栈（优先级 1）

| 层级 | 技术 |
|---|---|
| **前端** | Next.js 14+（React）、TypeScript、Tailwind CSS |
| **后端** | ASP.NET Core 8+（C#）、RESTful APIs |
| **ORM** | Entity Framework Core（EF Core） |
| **数据库** | PostgreSQL 16+ |
| **缓存** | Redis 7+ |
| **支付** | Stripe API（沙盒） |
| **商品数据** | PostgreSQL 种子数据（P1）；Square Catalogue API（P2） |

| **认证** | JWT（HS256）+ bcrypt |
| **Webhook** | ngrok |
| **容器化** | Docker、Docker Compose |
| **云服务** | AWS（EC2、RDS、ElastiCache、S3） |

### 扩展栈（优先级 2 & 3）

| 层级 | 技术 |
|---|---|
| **PWA** | Web App Manifest、Service Worker |
| **CSS** | CSS Grid、rem、自定义属性、媒体查询 |
| **状态管理** | React Context API（`AuthContext`、`CartContext`、`WishlistContext`、`ToastContext`） |
| **组件化** | 可复用组件库 + Custom Hooks |
| **图表** | ECharts（管理后台） |
| **API 文档** | Swagger / OpenAPI（Swashbuckle） |
| **测试（后端）** | xUnit |
| **测试（前端）** | Vitest + React Testing Library |
| **架构** | Controller → Service → Mapper → Entity |
| **设计模式** | 工厂模式、策略模式 |
| **数据库规范** | 阿里巴巴开发规范 |
| **CI/CD** | GitHub Actions |

### 为什么选择这个技术栈？

**MVP 决策：**
- **ASP.NET Core** — 高性能、跨平台后端，内置依赖注入
- **EF Core** — .NET 官方 ORM，Code-First 迁移，LINQ 查询
- **Next.js** — SSR、文件路由、优化的 React 开发体验
- **PostgreSQL** — 强大的关系型数据库，支持复杂查询和事务
- **Redis** — 内存缓存，用于会话、购物车和近期订单
- **Stripe** — 行业标准支付，优秀的沙盒模式
- **Docker** — 一致的开发和部署环境

**工程实践：**
- **分层架构** — 关注点分离：Controller（HTTP）、Service（逻辑）、Mapper（数据）、Entity（模型）
- **工厂模式 & 策略模式** — 可扩展的支付提供商、清晰的对象创建
- **阿里巴巴规范** — 统一的数据库规范：`idx_表_字段` 命名、ID 用 `BIGINT`、金额用 `DECIMAL`

---

## 系统架构

```
┌─────────────┐     HTTPS      ┌─────────────────┐     REST API     ┌──────────────────┐
│   浏览器     │ ──────────────>│    Next.js       │ ───────────────>│  ASP.NET Core     │
│  (客户端)    │ <──────────────│   前端应用        │ <───────────────│  后端 API         │
└─────────────┘                └─────────────────┘                  └────────┬─────────┘
                                                                            │
                                                          ┌─────────────────┼─────────────────┐
                                                          │                 │                 │
                                                          ▼                 ▼                 ▼
                                                   ┌────────────┐   ┌────────────┐   ┌────────────┐
                                                   │ PostgreSQL │   │    Redis   │   │   Stripe   │
                                                   │ (订单、用户)│   │ (缓存、会话)│   │ (支付、    │
                                                   │            │   │            │   │  Webhook)  │
                                                   └────────────┘   └────────────┘   └────────────┘
                                                                                          ▲
                                                                                          │
                                                                                     ┌────┴────┐
                                                                                     │  ngrok  │
                                                                                     │ (隧道)   │
                                                                                     └─────────┘
```

---

## 快速开始

### 环境要求

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker & Docker Compose](https://www.docker.com/get-started)
- [PostgreSQL 16+](https://www.postgresql.org/download/)（或通过 Docker）
- [Redis 7+](https://redis.io/download/)（或通过 Docker）
- [Stripe CLI](https://stripe.com/docs/stripe-cli)（Webhook 测试）
- [ngrok](https://ngrok.com/)（暴露本地 Webhook 端点）

### Docker 快速启动

```bash
# 克隆仓库
git clone https://github.com/your-username/novacart.git
cd novacart

# 复制环境变量
cp .env.example .env
# 编辑 .env，填入 Stripe 密钥、数据库凭据等

# 启动所有服务（构建 + 后台运行）
docker compose up --build -d

# 停止所有服务
docker compose down

# 停止并删除所有数据（数据库、缓存）
docker compose down -v
```

### 服务访问地址

| 服务 | 地址 | 说明 |
|---|---|---|
| **前端** | http://localhost:3000 | Next.js 应用 |
| **后端 API** | http://localhost:5000 | ASP.NET Core Web API |
| **Swagger 文档** | http://localhost:5000/swagger | API 接口文档 |
| **健康检查** | http://localhost:5000/api/health | 后端运行状态 |
| **PostgreSQL** | localhost:5432 | 数据库（用户: postgres，密码: postgres） |
| **Redis** | localhost:6379 | 缓存 |

### Docker 常用命令

```bash
# 启动所有服务
docker compose up --build -d

# 查看服务状态
docker compose ps

# 查看日志（所有服务）
docker compose logs -f

# 查看日志（指定服务）
docker compose logs -f backend
docker compose logs -f frontend

# 重启某个服务
docker compose restart backend

# 重新构建某个服务
docker compose up --build -d backend

# 停止所有服务
docker compose down

# 停止并删除所有数据卷
docker compose down -v

# 进入运行中的容器
docker exec -it novacart-backend-1 bash
docker exec -it novacart-postgres-1 psql -U postgres -d novacart
```

### 手动搭建

#### 后端（ASP.NET Core）

```bash
cd backend
dotnet restore
dotnet ef database update
dotnet run --launch-profile "Development"
```

#### 前端（Next.js）

```bash
cd frontend
npm install
npm run dev
```

#### Stripe Webhook（使用 ngrok）

```bash
# 终端 1：启动 ngrok 隧道
ngrok http 5000

# 终端 2：转发 Stripe 事件
stripe listen --forward-to localhost:5000/api/webhooks/stripe
```

### 环境变量

在项目根目录创建 `.env` 文件：

```env
# 数据库
DATABASE_URL=postgresql://postgres:password@localhost:5432/novacart

# Redis
REDIS_URL=localhost:6379

# JWT
JWT_SECRET=your-super-secret-key-change-in-production
JWT_EXPIRY_HOURS=24

# Stripe（沙盒）
STRIPE_SECRET_KEY=sk_test_...
STRIPE_PUBLISHABLE_KEY=pk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...

# Square（沙盒 — P2）
SQUARE_ACCESS_TOKEN=EAAAl...
SQUARE_ENVIRONMENT=sandbox

# ngrok
NGROK_URL=https://your-ngrok-url.ngrok-free.app
```

---

## 项目结构

```
novacart/
├── frontend/                    # Next.js 应用
│   ├── src/
│   │   ├── app/                 # App Router 页面
│   │   │   ├── (auth)/          # 登录 & 注册
│   │   │   ├── products/        # 商品浏览
│   │   │   ├── cart/            # 购物车
│   │   │   ├── checkout/        # 结账流程
│   │   │   └── orders/          # 订单历史
│   │   ├── components/          # 可复用 UI 组件
│   │   ├── hooks/               # 自定义 React Hooks
│   │   ├── lib/                 # 工具函数 & API 客户端
│   │   ├── store/               # 状态管理（购物车、认证）
│   │   └── types/               # TypeScript 类型定义
│   ├── public/
│   ├── tailwind.config.ts
│   ├── next.config.ts
│   └── package.json
│
├── backend/                     # ASP.NET Core Web API
│   ├── Controllers/             # API 控制器
│   ├── Models/                  # 领域模型 & DTO
│   ├── Services/                # 业务逻辑层
│   ├── Data/                    # DbContext & 迁移
│   ├── Middleware/              # JWT 认证、错误处理
│   └── Program.cs               # 应用入口点
│
├── docker-compose.yml
├── Dockerfile.frontend
├── Dockerfile.backend
├── .env.example
└── README.md
```

---

## API 参考

### 认证

| 方法 | 端点 | 描述 |
|---|---|---|
| POST | `/api/auth/register` | 用户注册 |
| POST | `/api/auth/login` | 用户登录，返回 JWT + refresh token（HttpOnly Cookie） |
| POST | `/api/auth/refresh` | 轮换 access + refresh token |
| POST | `/api/auth/logout` | 吊销 refresh token 并清除 Cookie |

### 商品

| 方法 | 端点 | 描述 |
|---|---|---|
| GET | `/api/products` | 获取商品列表（分页） |
| GET | `/api/products/{id}` | 获取商品详情 |
| GET | `/api/products/search` | 关键词搜索商品 |

### 购物车

| 方法 | 端点 | 描述 |
|---|---|---|
| GET | `/api/cart` | 获取当前用户购物车 |
| POST | `/api/cart/items` | 添加商品到购物车 |
| PUT | `/api/cart/items/{id}` | 更新商品数量 |
| DELETE | `/api/cart/items/{id}` | 从购物车移除商品 |

### 结账与支付

| 方法 | 端点 | 描述 |
|---|---|---|
| POST | `/api/checkout` | 创建结账会话 |
| POST | `/api/webhooks/stripe` | Stripe Webhook 端点 |

### 订单

| 方法 | 端点 | 描述 |
|---|---|---|
| GET | `/api/orders` | 获取用户订单历史 |
| GET | `/api/orders/{id}` | 获取订单详情 |

---

## 部署方案

### AWS 架构

```
                    ┌─────────────────────────────────────────┐
                    │              AWS 云                      │
                    │                                         │
                    │   ┌──────────┐    ┌──────────────────┐  │
  用户 ────────────>│   │  EC2     │    │   RDS            │  │
  (HTTPS)           │   │  (应用)  │───>│   (PostgreSQL)   │  │
                    │   └──────────┘    └──────────────────┘  │
                    │        │                                 │
                    │        │          ┌──────────────────┐  │
                    │        └─────────>│   ElastiCache    │  │
                    │                   │   (Redis)        │  │
                    │                   └──────────────────┘  │
                    │                                         │
                    │                   ┌──────────────────┐  │
                    │                   │   S3             │  │
                    │                   │  (静态资源)       │  │
                    │                   └──────────────────┘  │
                    └─────────────────────────────────────────┘
```

### Docker Compose（生产环境）

```bash
docker compose -f docker-compose.prod.yml up -d
```

---

## 开源协议

本项目基于 MIT 协议开源。详见 [LICENCE](LICENCE) 文件。

---

## 致谢

- [Stripe 文档](https://stripe.com/docs)
- [Square 开发者文档](https://developer.squareup.com/docs)
- [Next.js 文档](https://nextjs.org/docs)
- [ASP.NET Core 文档](https://learn.microsoft.com/en-us/aspnet/core/)
- [UNSW COMP3900/9900 毕业设计项目](https://www.cse.unsw.edu.au/~cs3900/) — Project #14
