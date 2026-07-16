# Novacart — 技术知识笔记

> 涵盖项目规划阶段讨论的所有技术、概念和架构决策的综合笔记。
> 以 Senior Software Engineer 技能水平为目标，系统性学习。

---

## 📋 Checklist 学习法（三层结构）

```
第一层：10 个领域（Senior 技能地图）
  └── 第二层：每个领域的具体知识点（~315 个，打勾追踪）
        └── 第三层：每个知识点的深度学习（按以下 4 步完成）
```

### 第三层学习模板

对第二层的**每一个知识点**，按以下 4 步完成学习，全部完成后打勾：

- [ ] **① 背景与应用场景**
  - 这个知识点解决什么问题？
  - 为什么需要它？没有它会怎样？
  - 在什么场景下使用？（结合 Novacart 项目或真实工作场景）

- [ ] **② 替代方案与易混淆概念**
  - 它的替代方案（Alternative）是什么？各自的优缺点？
  - 容易混淆的概念有哪些？如何区分？
  - 为什么选它而不选其他？（技术选型的权衡）

- [ ] **③ 示例代码 / 实际操作**
  - 写一段能跑的示例代码（不要复制粘贴，要自己手写理解）
  - 如果是操作类知识（如 Docker、Git），写出实际命令和操作步骤
  - 结合 Novacart 项目，写出实际应用的代码片段

- [ ] **④ 面试常考 5 题**
  - 场景题 1：「你的系统中如何使用 XX？为什么这样设计？」
  - 场景题 2：「如果 XX 出了问题，你怎么排查和解决？」
  - 快问快答 1：「XX 的核心原理是什么？一句话说明。」
  - 快问快答 2：「XX 和 YY 的区别是什么？」
  - 综合题：「给你一个需求，你会如何设计？考虑哪些因素？」

### 示例：以「Redis 缓存穿透」为例

```markdown
## Redis 缓存穿透

### ① 背景与应用场景
- **问题**：查询一个不存在的数据，缓存永远不命中，每次都打到数据库
- **场景**：恶意用户用不存在的 user_id 频繁请求接口
- **影响**：数据库压力暴增，可能导致服务崩溃

### ② 替代方案与易混淆概念
- **缓存穿透** vs **缓存击穿** vs **缓存雪崩**：
  - 穿透：查询不存在的数据
  - 击穿：热点 key 过期，大量请求同时打到 DB
  - 雪崩：大量 key 同时过期
- **解决方案对比**：
  - 布隆过滤器：空间效率高，但有假阳性
  - 缓存空值：简单，但浪费内存
  - 接口校验：在缓存之前先校验参数合法性

### ③ 示例代码
```csharp
public async Task<User?> GetUserAsync(int userId)
{
    // 1. 参数校验（防止恶意输入）
    if (userId <= 0) return null;

    // 2. 查缓存
    var cacheKey = $"user:{userId}";
    var cached = await _redis.GetAsync<User>(cacheKey);
    if (cached is not null) return cached;  // 命中（包括空值标记）

    // 3. 查数据库
    var user = await _db.Users.FindAsync(userId);

    // 4. 写缓存（不存在的数据也缓存，设短 TTL）
    if (user is null)
        await _redis.SetAsync(cacheKey, "NULL", TimeSpan.FromMinutes(5));
    else
        await _redis.SetAsync(cacheKey, user, TimeSpan.FromHours(1));

    return user;
}
```

### ④ 面试常考 5 题
1. **场景题 1**：你的电商系统商品查询接口被恶意刷不存在的 ID，导致数据库 CPU 飙到 90%，你怎么处理？
2. **场景题 2**：你用了缓存空值方案，但发现 Redis 内存增长很快，怎么优化？
3. **快问快答 1**：缓存穿透的核心是什么？→ 查询不存在的数据，缓存永远 miss。
4. **快问快答 2**：缓存穿透和缓存击穿的区别？→ 穿透是数据不存在，击穿是热点 key 过期。
5. **综合题**：设计一个高并发的商品详情页接口，要考虑哪些缓存策略？如何防止穿透、击穿、雪崩？
```

### 学习进度追踪

在第二层的每个知识点前用 `- [x]` 标记完成状态：

```markdown
- [x] B+ Tree 索引：工作原理、范围查询、最左前缀原则     ✅ 4/4 步完成
- [ ] Hash 索引：等值查询、不支持范围                      ⬜ 2/4 步完成
- [ ] 覆盖索引（Covering Index）：查询字段全在索引中        ⬜ 未开始
```

---

## 🗺️ Senior 技能地图（第一层目录）

> Senior 不是"会用多少框架"，而是"能设计系统、能解决问题、能做技术决策"。
> 以下 10 个领域是 Senior 面试和实际工作的核心能力。

### 领域 1：系统设计与架构（System Design）

| 主题 | 说明 | 优先级 |
|---|---|---|
| [架构决策](#架构决策) | 单体 vs 微服务、分层架构、MonolithFirst | ⭐⭐⭐ |
| [微服务概念](#微服务概念) | 服务拆分、服务发现、API 网关、熔断 | ⭐⭐⭐ |
| 分布式系统基础 | CAP 定理、一致性模型、分区容错 | ⭐⭐⭐ |
| 高可用设计 | 主从复制、负载均衡、故障转移、降级 | ⭐⭐⭐ |
| 容量规划 | QPS 估算、存储估算、带宽估算 | ⭐⭐ |
| 架构演进 | 从单体到微服务的迁移策略 | ⭐⭐ |
| CQRS / Event Sourcing | 读写分离、事件溯源 | ⭐ |

### 领域 2：数据库与存储（Database & Storage）

| 主题 | 说明 | 优先级 |
|---|---|---|
| [数据库与缓存](#数据库与缓存) | PostgreSQL、Redis、阿里巴巴规范 | ⭐⭐⭐ |
| 索引优化 | B+ Tree、Hash、GIN、GiST、覆盖索引、联合索引 | ⭐⭐⭐ |
| 事务与隔离级别 | ACID、脏读、幻读、MVCC、乐观锁/悲观锁 | ⭐⭐⭐ |
| 慢查询优化 | EXPLAIN 分析、索引失效场景、SQL 调优 | ⭐⭐⭐ |
| [分布式事务](#分布式事务) | AT/TCC/Saga 模式、最终一致性 | ⭐⭐ |
| [搜索引擎](#搜索引擎) | ElasticSearch、倒排索引、分词 | ⭐⭐ |
| NoSQL 选型 | MongoDB、Redis、Cassandra 的适用场景 | ⭐⭐ |
| 数据库分片 | 水平/垂直拆分、分片键选择、跨分片查询 | ⭐ |
| 数据库连接池 | 连接池配置、连接泄漏排查 | ⭐⭐ |

### 领域 3：后端开发（Backend Development）

| 主题 | 说明 | 优先级 |
|---|---|---|
| [后端技术](#后端技术) | ASP.NET Core、EF Core、Swagger | ⭐⭐⭐ |
| RESTful API 设计 | 命名规范、版本控制、HATEOAS、分页 | ⭐⭐⭐ |
| [认证与安全](#认证与安全) | JWT、OAuth2、RBAC、密码哈希 | ⭐⭐⭐ |
| [设计模式](#设计模式) | 策略、工厂、观察者、单例、仓储 | ⭐⭐⭐ |
| 依赖注入 | IoC 容器、生命周期（Singleton/Scoped/Transient） | ⭐⭐⭐ |
| 异步编程 | async/await、Task、死锁排查、ConfigureAwait | ⭐⭐⭐ |
| 错误处理 | 全局异常处理、错误码规范、重试策略 | ⭐⭐⭐ |
| 限流与熔断 | 令牌桶、滑动窗口、Polly、Sentinel | ⭐⭐ |
| [消息队列与异步处理](#消息队列与异步处理) | RabbitMQ、Kafka、死信队列、消息幂等 | ⭐⭐ |
| gRPC / GraphQL | 何时用 REST vs gRPC vs GraphQL | ⭐ |

### 领域 4：前端开发（Frontend Development）

| 主题 | 说明 | 优先级 |
|---|---|---|
| [前端技术](#前端技术) | Next.js、React、TypeScript | ⭐⭐⭐ |
| 状态管理 | Context、Zustand、Redux 的选型 | ⭐⭐⭐ |
| [性能优化](#性能优化) | 懒加载、代码分割、虚拟列表、防抖节流 | ⭐⭐⭐ |
| [CSS 与响应式设计](#css-与响应式设计) | CSS Grid、Flexbox、rem、媒体查询 | ⭐⭐ |
| [PWA](#pwa) | Service Worker、离线缓存、Manifest | ⭐⭐ |
| SSR / SSG / ISR | 服务端渲染、静态生成、增量渲染的区别和选型 | ⭐⭐ |
| Web 安全 | XSS、CSRF、CSP、CORS、SRI | ⭐⭐⭐ |
| 浏览器渲染原理 | 关键渲染路径、重排重绘、合成层 | ⭐ |

### 领域 5：测试与质量（Testing & Quality）

| 主题 | 说明 | 优先级 |
|---|---|---|
| [测试](#测试) | xUnit、Vitest、React Testing Library | ⭐⭐⭐ |
| 测试策略 | 测试金字塔（单测 > 集成 > E2E）、测试覆盖率 | ⭐⭐⭐ |
| Mock 与 Stub | 何时 Mock、如何 Mock 外部依赖 | ⭐⭐⭐ |
| TDD / BDD | 测试驱动开发、行为驱动开发 | ⭐⭐ |
| 压力测试 | JMeter、k6、Locust | ⭐⭐ |
| 代码质量 | Code Review 流程、静态分析、Lint | ⭐⭐⭐ |
| 混沌工程 | Chaos Monkey、故障注入 | ⭐ |

### 领域 6：DevOps 与运维（DevOps & Operations）

| 主题 | 说明 | 优先级 |
|---|---|---|
| [DevOps 与部署](#devops-与部署) | Docker、AWS、CI/CD | ⭐⭐⭐ |
| 容器编排 | Docker Compose → Kubernetes 的演进 | ⭐⭐ |
| 基础设施即代码 | Terraform、CloudFormation | ⭐⭐ |
| 日志与监控 | ELK Stack、Grafana、Prometheus、CloudWatch | ⭐⭐⭐ |
| 链路追踪 | OpenTelemetry、Jaeger、SkyWalking | ⭐⭐ |
| 告警与值班 | 告警规则、P0/P1/P2 分级、On-call | ⭐⭐ |
| 蓝绿/金丝雀部署 | 零停机部署策略 | ⭐⭐ |
| Git 工作流 | GitFlow、Trunk-Based、PR Review 流程 | ⭐⭐⭐ |

### 领域 7：性能工程（Performance Engineering）

| 主题 | 说明 | 优先级 |
|---|---|---|
| [性能优化](#性能优化) | 懒加载、线程池、代码分割 | ⭐⭐⭐ |
| 性能分析工具 | dotnet-trace、BenchmarkDotNet、Lighthouse | ⭐⭐⭐ |
| 内存管理 | GC 机制、内存泄漏排查、IDisposable | ⭐⭐ |
| 并发编程 | 锁、Semaphore、Channel、并发集合 | ⭐⭐ |
| CDN 与缓存策略 | HTTP 缓存头、CDN 配置、缓存失效 | ⭐⭐ |
| 数据库性能 | 连接池、批量操作、N+1 查询问题 | ⭐⭐⭐ |

### 领域 8：安全（Security）

| 主题 | 说明 | 优先级 |
|---|---|---|
| [认证与安全](#认证与安全) | JWT、bcrypt、HttpOnly Cookie | ⭐⭐⭐ |
| OWASP Top 10 | SQL 注入、XSS、CSRF、SSRF、不安全的反序列化 | ⭐⭐⭐ |
| 密钥管理 | Secret 管理、环境变量、Vault | ⭐⭐ |
| 网络安全 | HTTPS、TLS、证书管理 | ⭐⭐ |
| 安全编码 | 输入验证、输出编码、最小权限原则 | ⭐⭐⭐ |
| 合规 | GDPR、PCI DSS（支付）、SOC 2 | ⭐ |

### 领域 9：软技能与工程文化（Soft Skills & Engineering Culture）

| 主题 | 说明 | 优先级 |
|---|---|---|
| Code Review | 如何做 Code Review、如何写好的 PR | ⭐⭐⭐ |
| 技术文档 | ADR（架构决策记录）、RFC、设计文档 | ⭐⭐⭐ |
| 技术选型方法论 | 如何评估和选择技术方案 | ⭐⭐⭐ |
| 故障复盘 | Post-mortem、根因分析、改进措施 | ⭐⭐ |
| 技术分享 | 如何做技术分享、写技术博客 | ⭐⭐ |
| 跨团队协作 | API 契约、技术方案评审、需求对齐 | ⭐⭐ |
| 导师制 | 如何带 Junior、知识传承 | ⭐ |

### 领域 10：支付与电商专项（Domain Knowledge）

| 主题 | 说明 | 优先级 |
|---|---|---|
| [支付集成](#支付集成) | Stripe、Webhook、令牌化 | ⭐⭐⭐ |
| 幂等性 | 支付回调幂等、重复扣款防护 | ⭐⭐⭐ |
| 对账 | 支付对账、异常处理、退款流程 | ⭐⭐ |
| 电商领域模型 | 商品、SKU、库存、订单状态机 | ⭐⭐ |

---

## 📖 第二层：详细知识点

> 以下是每个领域的具体知识点清单。逐项学习，打勾确认。

---

### 领域 1：系统设计与架构

#### 1.1 架构决策
- [ ] MonolithFirst 模式：先单体后拆分的策略和时机
- [ ] 模块化单体（Modular Monolith）：模块边界、独立 Schema、事件驱动解耦
- [ ] 绞杀者模式（Strangler Fig）：渐进式迁移旧系统到新架构
- [ ] 架构决策记录（ADR）：如何写 ADR、模板格式、决策权衡分析

#### 1.2 微服务架构
- [ ] 服务拆分原则：按领域（DDD Bounded Context）/ 按能力 / 按数据所有权
- [ ] 服务发现：Consul、Eureka、Kubernetes Service 的工作原理
- [ ] API 网关：路由、限流、认证、协议转换（YARP、Ocelot、Kong）
- [ ] 熔断器模式：Open → Half-Open → Closed 状态转换、Polly 实现
- [ ] 服务间通信：同步（HTTP/gRPC）vs 异步（消息队列）的选型
- [ ] Saga 模式：编排式（Orchestration）vs 协同式（Choreography）
- [ ] 服务网格（Service Mesh）：Istio、Linkerd 的概念和适用场景

#### 1.3 分布式系统基础
- [ ] CAP 定理：一致性、可用性、分区容错的三角权衡
- [ ] PACELC 模型：CAP 的扩展，考虑延迟
- [ ] 一致性模型：强一致性、最终一致性、因果一致性
- [ ] 共识算法：Raft、Paxos 的基本原理（不需要手写，但要理解 Leader 选举）
- [ ] 向量时钟（Vector Clocks）：分布式事件排序
- [ ] 分布式 ID 生成：Snowflake、UUID v7、数据库自增 vs 全局唯一

#### 1.4 高可用设计
- [ ] 主从复制（Leader-Follower）：异步复制 vs 同步复制、复制延迟
- [ ] 多主复制（Multi-Leader）：冲突解决策略
- [ ] 负载均衡算法：轮询、加权轮询、最少连接、一致性哈希
- [ ] 健康检查：主动探测 vs 被动探测、优雅下线
- [ ] 故障转移（Failover）：手动 vs 自动、脑裂问题
- [ ] 优雅降级：核心功能优先、非核心功能熔断、返回缓存/默认值
- [ ] 限流策略：令牌桶、漏桶、滑动窗口、固定窗口

#### 1.5 容量规划
- [ ] Little's Law：L = λW（系统中的平均请求数 = 到达率 × 平均处理时间）
- [ ] QPS / TPS 估算：如何从用户量推算系统负载
- [ ] 存储估算：数据量增长预测、冷热数据分离
- [ ] 带宽估算：图片、视频、API 响应的带宽需求
- [ ] 延迟百分位：p50、p95、p99 的含义和用途
- [ ] 压测方法论：基准测试、负载测试、压力测试、浸泡测试

#### 1.6 CQRS 与事件溯源
- [ ] CQRS：命令（写）和查询（读）分离、独立模型
- [ ] 事件溯源（Event Sourcing）：存储事件而非状态、事件重放
- [ ] 投影（Projection）：从事件流构建读模型
- [ ] 快照（Snapshot）：避免事件重放过慢

---

### 领域 2：数据库与存储

#### 2.1 索引优化
- [ ] B+ Tree 索引：工作原理、范围查询、最左前缀原则
- [ ] Hash 索引：等值查询、不支持范围
- [ ] 覆盖索引（Covering Index）：查询字段全在索引中，无需回表
- [ ] 联合索引（Composite Index）：列顺序、索引失效场景
- [ ] 部分索引（Partial Index）：只索引满足条件的行
- [ ] 表达式索引（Expression Index）：对函数结果建索引
- [ ] 索引失效：函数操作、隐式类型转换、LIKE '%xx'、OR 条件

#### 2.2 事务与隔离级别
- [ ] ACID：原子性、一致性、隔离性、持久性
- [ ] 隔离级别：Read Uncommitted → Read Committed → Repeatable Read → Serializable
- [ ] 脏读、不可重复读、幻读的区别和产生原因
- [ ] MVCC（多版本并发控制）：PostgreSQL 的实现原理、快照隔离
- [ ] 乐观锁 vs 悲观锁：版本号、SELECT FOR UPDATE、适用场景
- [ ] 死锁：产生条件、检测方法、预防策略

#### 2.3 慢查询优化
- [ ] EXPLAIN / EXPLAIN ANALYZE：如何读懂执行计划
- [ ] Seq Scan vs Index Scan vs Index Only Scan
- [ ] N+1 查询问题：如何发现、如何解决（JOIN / 预加载）
- [ ] 子查询 vs JOIN vs CTE 的性能差异
- [ ] 批量操作：INSERT 批量、UPDATE 批量、避免逐行操作
- [ ] 查询缓存：Redis 缓存查询结果、缓存失效策略

#### 2.4 数据库设计
- [ ] 范式化（1NF → 2NF → 3NF）vs 反范式化
- [ ] 数据类型选择：BIGINT vs INT、DECIMAL vs FLOAT、VARCHAR vs TEXT
- [ ] 命名规范：snake_case、表名复数/单数、索引命名
- [ ] 软删除（Soft Delete）vs 硬删除：is_deleted 字段、数据恢复
- [ ] 审计字段：created_at、updated_at、created_by
- [ ] JSON/JSONB 字段：何时用、如何索引、性能影响

#### 2.5 Redis 深入
- [ ] 数据类型：String、Hash、List、Set、Sorted Set、Stream、HyperLogLog
- [ ] 内存淘汰策略：LRU、LFU、TTL、noeviction
- [ ] 持久化：RDB 快照 vs AOF 日志、混合持久化
- [ ] Redis Cluster：分片原理、一致性哈希、数据迁移
- [ ] Redis Sentinel：高可用、自动故障转移
- [ ] 缓存穿透、缓存击穿、缓存雪崩的区别和解决方案
- [ ] 分布式锁：Redlock 算法、锁续期、可重入锁

#### 2.6 连接池
- [ ] 连接池参数：最小连接数、最大连接数、空闲超时
- [ ] 连接泄漏排查：未关闭的连接、using 语句
- [ ] 连接池监控：活跃连接数、等待队列长度
- [ ] PgBouncer：PostgreSQL 连接池中间件

---

### 领域 3：后端开发

#### 3.1 RESTful API 设计
- [ ] 命名规范：名词复数、小写、连字符（/api/order-items）
- [ ] HTTP 方法语义：GET（幂等）、POST（非幂等）、PUT（全量替换）、PATCH（部分更新）、DELETE
- [ ] 状态码规范：2xx 成功、3xx 重定向、4xx 客户端错误、5xx 服务端错误
- [ ] 版本控制：URL 版本（/v1/）vs Header 版本（Accept-Version）
- [ ] 分页：Offset vs Cursor 分页、总数返回、Link Header
- [ ] 过滤与排序：查询参数规范（?status=active&sort=-created_at）
- [ ] HATEOAS：超媒体驱动的 REST（了解概念即可）
- [ ] API 限流：RateLimit 响应头、429 Too Many Requests

#### 3.2 异步编程
- [ ] async/await：状态机原理、SynchronizationContext
- [ ] Task vs ValueTask：何时用哪个
- [ ] 死锁排查：.Result、.Wait()、ConfigureAwait(false)
- [ ] CancellationToken：超时取消、优雅关闭
- [ ] Parallel.ForEach / Task.WhenAll：并行执行
- [ ] Channel<T>：生产者-消费者模式
- [ ] IAsyncEnumerable：异步流式数据

#### 3.3 错误处理
- [ ] 全局异常处理中间件：异常类型 → 状态码映射
- [ ] 自定义异常类：BusinessException、NotFoundException
- [ ] 错误响应格式：RFC 7807 Problem Details
- [ ] 重试策略：指数退避（Exponential Backoff）、抖动（Jitter）
- [ ] 熔断策略：连续失败 N 次后熔断、半开探测
- [ ] 降级策略：返回缓存数据、默认值、友好提示

#### 3.4 依赖注入
- [ ] IoC 容器原理：控制反转、依赖注入（构造函数注入）
- [ ] 生命周期：Singleton（全局唯一）→ Scoped（请求内唯一）→ Transient（每次新建）
- [ ] 生命周期陷阱：Singleton 不能注入 Scoped 服务
- [ ] 工厂模式注入：IServiceProvider、ActivatorUtilities
- [ ] 装饰器模式注入：动态替换实现

#### 3.5 消息队列深入
- [ ] RabbitMQ：Exchange（Direct/Topic/Fanout/Headers）、Queue、Binding
- [ ] 消息确认：Ack/Nack、手动确认 vs 自动确认
- [ ] 死信队列（DLQ）：消息消费失败后的处理
- [ ] 消息幂等：消费端去重、唯一消息 ID
- [ ] 消息顺序性：分区有序 vs 全局有序
- [ ] Kafka：Topic、Partition、Consumer Group、Offset 管理
- [ ] 最终一致性：如何保证跨服务数据一致

---

### 领域 4：前端开发

#### 4.1 Next.js 深入
- [ ] App Router：布局嵌套、加载 UI、错误边界
- [ ] Server Components vs Client Components：何时用哪个
- [ ] Server Actions：表单提交、数据变更
- [ ] Middleware：认证检查、重定向、A/B 测试
- [ ] 数据获取：fetch 缓存、revalidate、no-store
- [ ] Image 组件：自动优化、懒加载、响应式图片
- [ ] 国际化：i18n 路由、动态导入语言包

#### 4.2 React 深入
- [ ] 虚拟 DOM 与 Reconciliation：Diff 算法、Key 的作用
- [ ] Hooks 原理：useState、useEffect、useRef、useMemo、useCallback
- [ ] 闭包陷阱：stale closure、如何避免
- [ ] 并发特性：Suspense、useTransition、useDeferredValue
- [ ] 性能优化：React.memo、useMemo、useCallback 的正确使用
- [ ] 错误边界：ErrorBoundary、fallback UI

#### 4.3 TypeScript
- [ ] 基本类型：string、number、boolean、any、unknown、never
- [ ] 接口 vs 类型别名：何时用 interface、何时用 type
- [ ] 泛型：泛型函数、泛型约束、条件类型
- [ ] 工具类型：Partial、Required、Pick、Omit、Record、Exclude
- [ ] 类型守卫：typeof、instanceof、自定义类型守卫
- [ ] 模块声明：.d.ts 文件、声明合并

#### 4.4 状态管理
- [ ] Context API：Provider、useContext、性能问题（不必要的重渲染）
- [ ] Zustand：create、subscribe、devtools、persist
- [ ] Redux Toolkit：createSlice、RTK Query
- [ ] React Query / TanStack Query：服务端状态管理、缓存、乐观更新
- [ ] URL 状态：搜索参数、路由状态

#### 4.5 性能优化
- [ ] 懒加载：dynamic import、React.lazy、Suspense
- [ ] 代码分割：路由级分割、组件级分割
- [ ] 虚拟列表：react-window、react-virtuoso（大列表渲染）
- [ ] 防抖（Debounce）与节流（Throttle）：搜索输入、滚动事件
- [ ] Web Workers：CPU 密集型任务卸载到后台线程
- [ ] 图片优化：WebP/AVIF 格式、响应式图片、CDN

#### 4.6 Web 安全（前端）
- [ ] XSS：反射型、存储型、DOM 型、防御（输出编码、CSP）
- [ ] CSRF：攻击原理、防御（SameSite Cookie、CSRF Token）
- [ ] CSP（Content Security Policy）：限制资源加载来源
- [ ] CORS：同源策略、预检请求、Access-Control 头
- [ ] SRI（Subresource Integrity）：验证外部资源完整性

#### 4.7 浏览器渲染原理
- [ ] 关键渲染路径：HTML → DOM → CSSOM → Render Tree → Layout → Paint → Composite
- [ ] 重排（Reflow）vs 重绘（Repaint）：什么操作触发、如何减少
- [ ] 合成层（Compositing Layers）：will-change、transform、opacity
- [ ] 长任务（Long Tasks）：如何检测、如何拆分

---

### 领域 5：测试与质量

#### 5.1 测试策略
- [ ] 测试金字塔：大量单测 → 适量集成测试 → 少量 E2E
- [ ] 测试覆盖率：行覆盖、分支覆盖、目标 80%+
- [ ] FIRST 原则：Fast、Isolated、Repeatable、Self-validating、Timely
- [ ] AAA 模式：Arrange（准备）→ Act（执行）→ Assert（断言）
- [ ] 测试命名：Method_Scenario_ExpectedResult

#### 5.2 Mock 与 Stub
- [ ] Test Double 类型：Dummy、Stub、Spy、Mock、Fake
- [ ] 何时 Mock：外部依赖（数据库、HTTP、文件系统）
- [ ] 何时不 Mock：纯逻辑、值对象、简单计算
- [ ] .NET：Moq、NSubstitute
- [ ] JavaScript：Vitest mock、MSW（Mock Service Worker）

#### 5.3 压力测试
- [ ] JMeter：GUI 录制、参数化、分布式压测
- [ ] k6：脚本编写、场景配置、指标解读
- [ ] Locust：Python 编写、Web UI
- [ ] 关键指标：吞吐量（RPS）、延迟（p50/p95/p99）、错误率

#### 5.4 代码质量
- [ ] Code Review Checklist：正确性、安全性、性能、可读性、测试
- [ ] 静态分析：SonarQube、ESLint、Stylelint
- [ ] 代码格式化：Prettier、EditorConfig
- [ ] Conventional Commits：feat/fix/chore/test/docs 前缀
- [ ] PR 规范：小 PR、清晰描述、关联 Issue

---

### 领域 6：DevOps 与运维

#### 6.1 Docker 深入
- [ ] Dockerfile 最佳实践：多阶段构建、层缓存、最小镜像
- [ ] Docker Compose：服务编排、网络、卷挂载、环境变量
- [ ] 镜像安全：扫描漏洞、最小基础镜像（Alpine、distroless）
- [ ] 容器日志：stdout/stderr、日志驱动

#### 6.2 Kubernetes 基础
- [ ] Pod：最小调度单元、多容器 Pod
- [ ] Service：ClusterIP、NodePort、LoadBalancer
- [ ] Deployment：滚动更新、回滚、HPA 自动扩缩
- [ ] Ingress：路由规则、TLS 终止
- [ ] ConfigMap / Secret：配置和密钥管理

#### 6.3 日志与监控
- [ ] 结构化日志：JSON 格式、日志级别、关联 ID
- [ ] ELK Stack：Elasticsearch + Logstash + Kibana
- [ ] Prometheus + Grafana：指标采集、仪表盘、告警
- [ ] RED 方法：Rate（请求率）、Error（错误率）、Duration（延迟）
- [ ] USE 方法：Utilisation（利用率）、Saturation（饱和度）、Errors

#### 6.4 链路追踪
- [ ] OpenTelemetry：Traces、Spans、Context Propagation
- [ ] Jaeger / Zipkin：分布式追踪可视化
- [ ] 关联 ID（Correlation ID）：跨服务请求追踪

#### 6.5 部署策略
- [ ] 蓝绿部署：两套环境、瞬间切换、快速回滚
- [ ] 金丝雀部署：小比例流量验证、逐步放量
- [ ] 滚动更新：逐步替换旧实例
- [ ] Feature Flag：功能开关、灰度发布

#### 6.6 Git 工作流
- [ ] GitFlow：main/develop/feature/release/hotfix 分支
- [ ] Trunk-Based：主干开发、短生命周期分支
- [ ] PR Review：如何写 PR 描述、如何做 Code Review
- [ ] 语义化版本：SemVer（major.minor.patch）

---

### 领域 7：性能工程

#### 7.1 性能分析工具
- [ ] dotnet-trace：CPU 采样、事件跟踪
- [ ] BenchmarkDotNet：微基准测试、内存诊断
- [ ] Lighthouse：Web 性能评分（LCP、FID、CLS）
- [ ] Chrome DevTools：Performance 面板、Memory 面板

#### 7.2 内存管理
- [ ] GC 机制：Generation 0/1/2、大对象堆（LOH）
- [ ] IDisposable 模式：Dispose、using 语句、Finalizer
- [ ] 内存泄漏排查：dotnet-dump、dotnet-gcroot
- [ ] WeakReference：缓存场景、避免内存泄漏
- [ ] ArrayPool / MemoryPool：减少数组分配

#### 7.3 并发编程
- [ ] lock / Monitor：互斥锁、死锁预防
- [ ] SemaphoreSlim：信号量、限制并发数
- [ ] ReaderWriterLockSlim：读写锁
- [ ] ConcurrentDictionary / ConcurrentQueue：线程安全集合
- [ ] Channel<T>：生产者-消费者、有界/无界
- [ ] Interlocked：原子操作

#### 7.4 数据库性能
- [ ] 连接池配置：最大连接数、连接超时
- [ ] 批量操作：EF Core BulkExtensions、COPY 命令
- [ ] N+1 问题：Include/ThenInclude、投影查询
- [ ] 读写分离：主库写、从库读、一致性考虑
- [ ] 查询结果缓存：Redis 缓存、缓存失效策略

---

### 领域 8：安全

#### 8.1 OWASP Top 10
- [ ] SQL 注入：参数化查询、ORM 防护、输入验证
- [ ] XSS：输出编码、CSP 头、HttpOnly Cookie
- [ ] CSRF：SameSite Cookie、CSRF Token、双重提交
- [ ] SSRF：白名单验证、禁止内网访问
- [ ] 不安全的反序列化：避免反序列化不可信数据
- [ ] 敏感数据暴露：加密存储、传输加密、日志脱敏
- [ ] 访问控制失效：最小权限、RBAC、资源级权限

#### 8.2 密钥管理
- [ ] 环境变量：开发环境 .env、生产环境 Secret Manager
- [ ] Azure Key Vault / AWS Secrets Manager：集中管理密钥
- [ ] 密钥轮换：定期更换、自动轮换
- [ ] 密钥访问控制：最小权限原则、审计日志

#### 8.3 安全编码
- [ ] 输入验证：白名单验证、长度限制、类型检查
- [ ] 输出编码：HTML 编码、URL 编码、JavaScript 编码
- [ ] SQL 注入防护：参数化查询、存储过程、ORM
- [ ] 日志安全：不记录密码、Token、银行卡号
- [ ] 依赖安全：npm audit、dotnet list package --vulnerable

---

### 领域 9：软技能与工程文化

#### 9.1 Code Review
- [ ] Review Checklist：功能正确性、边界情况、安全漏洞、性能问题、代码风格
- [ ] 如何给反馈：具体、可操作、对事不对人
- [ ] PR 大小：一个 PR 只做一件事、控制在 200-400 行
- [ ] 自审（Self-Review）：提交前自己先 Review 一遍

#### 9.2 技术文档
- [ ] ADR（架构决策记录）：标题、背景、决策、后果
- [ ] RFC（征求意见稿）：提案流程、讨论、批准
- [ ] API 文档：OpenAPI 规范、Swagger UI
- [ ] 架构图：C4 模型（Context → Container → Component → Code）
- [ ] README：项目简介、快速开始、API 参考

#### 9.3 技术选型方法论
- [ ] 评估维度：性能、社区活跃度、学习曲线、维护成本、许可证
- [ ] POC（概念验证）：用最小代码验证关键技术点
- [ ] Spike：限时调研、输出结论
- [ ] 决策矩阵：列出候选方案、打分、加权

#### 9.4 故障复盘
- [ ] 无指责复盘（Blameless Post-mortem）：关注系统而非个人
- [ ] 5 Whys 分析法：追问根因
- [ ] 时间线记录：故障发现 → 响应 → 定位 → 修复 → 恢复
- [ ] 改进措施：短期修复、长期预防、责任人和截止日期

---

### 领域 10：支付与电商专项

#### 10.1 Stripe 支付
- [ ] Payment Intents：创建 → 确认 → 捕获流程
- [ ] Checkout Session：托管支付页面
- [ ] Webhook 签名验证：防止伪造、重放攻击
- [ ] 3D Secure（SCA）：强客户认证
- [ ] 退款：全额退款、部分退款、退款状态追踪
- [ ] 争议（Dispute）：处理流程、证据提交

#### 10.2 幂等性
- [ ] 幂等性概念：同一请求执行多次，结果相同
- [ ] 幂等键（Idempotency Key）：客户端生成、服务端存储
- [ ] 支付幂等：防止重复扣款
- [ ] Webhook 幂等：Stripe 可能重复发送同一事件

#### 10.3 电商领域模型
- [ ] 商品与 SKU：商品（Product）→ 规格（SKU）→ 库存（Inventory）
- [ ] 库存管理：下单预留、支付扣减、超时释放
- [ ] 订单状态机：pending → paid → processing → shipped → completed → cancelled
- [ ] 价格策略：原价、促销价、优惠券、满减

---

## 📚 项目相关笔记（按技术分类）

> 以下是 Novacart 项目涉及的具体技术笔记。

- [架构决策](#架构决策)
- [后端技术](#后端技术)
- [前端技术](#前端技术)
- [数据库与缓存](#数据库与缓存)
- [认证与安全](#认证与安全)
- [消息队列与异步处理](#消息队列与异步处理)
- [搜索引擎](#搜索引擎)
- [设计模式](#设计模式)
- [测试](#测试)
- [DevOps 与部署](#devops-与部署)
- [性能优化](#性能优化)
- [CSS 与响应式设计](#css-与响应式设计)
- [PWA](#pwa)
- [支付集成](#支付集成)
- [微服务概念](#微服务概念)
- [分布式事务](#分布式事务)
- [面试速查表](#面试速查表)
- [术语表](#术语表)
- [官方文档链接](#官方文档链接)
- [项目实战记录](#项目实战记录)

---

## 项目实战记录

> 记录项目搭建和开发过程中实际用到的技术、踩过的坑、学到的经验。

### 2026-06-04：项目骨架搭建

#### 1. Docker Compose 多服务编排

**做了什么：** 用一个 `docker-compose.yml` 编排 4 个服务：PostgreSQL、Redis、ASP.NET Core 后端、Next.js 前端。

**关键配置：**

```yaml
services:
  postgres:
    image: postgres:16-alpine
    healthcheck:                                    # ① 健康检查
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5

  backend:
    depends_on:
      postgres:
        condition: service_healthy                  # ② 等依赖健康后再启动
      redis:
        condition: service_healthy
    environment:                                    # ③ 环境变量覆盖配置
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;...
      - ConnectionStrings__Redis=redis:6379

  frontend:
    depends_on:
      - backend                                     # ④ 简单依赖（不等健康）
```

**学到的：**
- `healthcheck` 确保数据库真正就绪后再启动后端，避免连接失败
- `condition: service_healthy` 比简单的 `depends_on` 更可靠
- 容器间通信用**服务名**（`postgres`、`redis`、`backend`），不是 `localhost`
- 环境变量用 `__` 双下划线映射到 .NET 的嵌套配置（`ConnectionStrings:DefaultConnection`）

#### 2. Dockerfile 多阶段构建

**后端（ASP.NET Core）：**

```dockerfile
# Build stage — 用 SDK 镜像编译
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj ./           # 先复制 csproj
RUN dotnet restore          # 单独还原依赖（利用层缓存）
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage — 用轻量的 ASP.NET 镜像运行
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Novacart.Api.dll"]
```

**前端（Next.js）：**

```dockerfile
# Build stage
FROM node:20-alpine AS build
COPY package.json ./
RUN npm install             # 先装依赖（利用层缓存）
COPY . .
RUN npm run build

# Runtime stage
FROM node:20-alpine
COPY --from=build /app/.next ./.next
COPY --from=build /app/node_modules ./node_modules
CMD ["npm", "start"]
```

**学到的：**
- 多阶段构建让最终镜像更小（后端 ~210MB vs SDK ~800MB）
- **先复制依赖文件，再 COPY 全部代码** — 依赖不变时 `npm install` / `dotnet restore` 走缓存
- `.dockerignore` 排除 `node_modules`、`bin`、`obj` 避免发送不必要的上下文

#### 3. Next.js API 代理

**做了什么：** 前端 `/api/*` 请求自动转发到后端 `backend:5000`。

```javascript
// next.config.mjs
const nextConfig = {
  async rewrites() {
    return [
      {
        source: '/api/:path*',                       // 前端请求 /api/xxx
        destination: 'http://backend:5000/api/:path*', // 转发到后端
      },
    ];
  },
};
```

**学到的：**
- `rewrites` 是服务端代理，浏览器不感知后端地址，避免 CORS 问题
- 开发环境和 Docker 环境的后端地址不同：Docker 内用 `backend:5000`，本地开发用 `localhost:5000`
- 生产环境建议用 Nginx 反向代理，不依赖 Next.js rewrites

#### 4. ASP.NET Core 依赖注入配置

**做了什么：** 在 `Program.cs` 中注册 DbContext、Redis、CORS、Swagger。

```csharp
// PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis — 注册为单例（连接池复用）
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(config));

// CORS — 允许前端跨域访问
builder.Services.AddCors(options =>
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()));
```

**学到的：**
- `AddDbContext` 默认 Scoped 生命周期（每个请求一个实例）
- `IConnectionMultiplexer` 注册为 Singleton（Redis 连接池，全局复用）
- CORS 必须在 `app.UseCors()` 之后才能生效，顺序很重要
- `WithOrigins("http://localhost:3000")` 只允许前端域名，生产环境要改成真实域名

#### 5. 踩过的坑

| 坑 | 原因 | 解决 |
|---|---|---|
| Next.js 报错 `next.config.ts is not supported` | Next.js 14.2.0 不支持 `.ts` 配置文件 | 改成 `next.config.mjs` |
| 后端启动失败，连接不上 PostgreSQL | 容器还没就绪就启动了 | 加 `healthcheck` + `condition: service_healthy` |
| 前端请求后端 404 | Docker 内用 `localhost:5000` 无法访问后端容器 | 改成服务名 `backend:5000` |

#### 6. 命令速查

```bash
# 启动所有服务
docker compose up --build -d

# 查看服务状态
docker compose ps

# 查看日志（某个服务）
docker compose logs -f backend

# 停止所有服务
docker compose down

# 停止并删除数据卷
docker compose down -v

# 重新构建某个服务
docker compose up --build -d backend
```

---

### 2026-07-15：P2/P3 功能 — JWT Refresh、异步邮件、S3，以及它们暴露出来的 Bug

> 记录 P14 "preferred" 交付物所用的技术栈、端到端验证时遇到的真实坑，以及每个功能背后的设计决策。这些问题只有在真正跑起整套服务时才会出现——单元测试单独通过时是看不出来的。

#### 1. JWT Refresh Token — 轮换与重用检测

**技术栈：** PostgreSQL `refresh_tokens` 表 · 用 SHA-256 哈希的不透明 token · 双 HttpOnly cookie · `RandomNumberGenerator` 生成熵。

**做了什么：** 实现短期 access token（15 分钟）+ 长期 refresh token（7 天），带轮换。refresh 时旧 token 被撤销、签发新的一对；如果一个*已被撤销*的 token 再次出现，该用户的整个 token 家族全部撤销（重用检测——token 被盗的信号）。

**关键设计：不同 Path 的双 cookie**
```
novacart_jwt      → Path=/api        (15 分钟，每个 API 请求都带)
novacart_refresh  → Path=/api/auth   (7 天，只发给 auth 端点)
```
把 refresh cookie 的 `Path` 收窄到 `/api/auth`，意味着它永远不会随商品/购物车/订单请求发送——最小化暴露面。access token cookie 留在 `/api`，让 `[Authorize]` 端点通过 `OnMessageReceived` 读取。

**经验教训：**
- **哈希 refresh token，绝不存明文。** 和密码不同（bcrypt），refresh token 是高熵随机值，普通 SHA-256 哈希就足够且更快——当输入已经是 256 位随机时，bcrypt 的慢速 KDF 毫无意义。
- **重用检测是轮换的全部意义。** 如果只轮换不检测重用，被盗的 token 在一次轮换后仍可用。检测到*已撤销*的 token 被重用，就能假设被盗并撤销一切。
- **ClockSkew 很重要。** JWT bearer 校验用 `ClockSkew = 30s`。对 refresh 流程，你希望 access token *及时*过期——大的时钟偏移会削弱短 access token 寿命的意义。
- **前端 refresh 必须合并。** access token 过期时，多个在途请求会同时 401。没有共享的 `refreshPromise`，你会触发 N 个并行 refresh 调用（每次轮换都让前一个失效）。模块级单例 promise 把它们合并成一个。

**前端踩到的坑：**
```
// 错误 — N 个并发 401 触发 N 次 refresh，每次都让前一次失效
if (res.status === 401) { await refresh(); retry(); }

// 正确 — 一次在途 refresh；所有等待者共享其结果
let refreshPromise = null;
async function tryRefresh() {
  if (!refreshPromise) refreshPromise = doRefresh().finally(() => refreshPromise = null);
  return refreshPromise;
}
```

#### 2. 异步邮件队列 — `Channel<T>` + `BackgroundService`

**技术栈：** `System.Threading.Channels.Channel<EmailMessage>`（有界，256，`Wait` 背压）· `BackgroundService` worker · Scoped 的 `EmailService`（MailKit SMTP）。

**做了什么：** 把邮件发送从请求/webhook 处理中解耦。`PaymentService` 和 `AdminOrderService` 入队一个快照 `EmailMessage` 后立即返回；`EmailBackgroundWorker` 在后台线程消费队列。

**关键设计：worker 跑在自己的 DI scope 里**
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    await foreach (var message in _queue.ReadAllAsync(stoppingToken))
    {
        using var scope = _scopeFactory.CreateScope();           // ← 每条消息一个新 scope
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();
        // ... 发送 ...
    }
}
```
worker 是 **Singleton**（hosted service），但 `EmailService` 是 **Scoped**（每个请求需要 `AppDbContext`/config）。从 singleton 解析 scoped 服务会抛 captive-dependency 错误——所以 worker 必须通过 `IServiceScopeFactory` 为每条消息创建自己的 scope。

**经验教训：**
- **绝不跨 scope 传递 EF 实体。** webhook handler scope 里加载的 `Order`，到 worker 处理消息时已被 dispose。`EmailMessage` record 只捕获*基本类型字段*（邮箱、订单号、金额、状态）——是快照，不是引用。
- **有界 channel 施加背压。** `BoundedChannelFullMode.Wait` 让 `EnqueueAsync` 在满时阻塞，自然地在高负载下拖慢生产者，而不是用无界队列 OOM。
- **单次发送失败不能搞死 worker。** 每次迭代都包在 try/catch 里；worker 记日志后继续。否则一个坏 SMTP 响应会毒化整个队列。

**这次工作暴露的既有 bug：**
`docker-compose.prod.yml` 设的是 `Smtp__FromAddress`，但 `EmailService` 读的是 `Smtp:FromEmail`——生产环境的发件人地址被静默忽略。两个 key 不匹配。修复为同时读两者：`config["Smtp:FromEmail"] ?? config["Smtp:FromAddress"]`。

#### 3. 带 LocalStack 的 S3 对象存储 — 无需 AWS 账号

**技术栈：** `AWSSDK.S3` · `IS3StorageService`（配置驱动：LocalStack 或真实 AWS）· presigned PUT/GET URL · Docker 里的 `localstack/localstack:3.0`。

**做了什么：** admin 直接上传商品图片到 S3（浏览器 → presigned PUT URL → 存返回的 URL）。后端从不代理文件体。LocalStack 在本地模拟 S3，所以开发不需要 AWS 账号/凭证。

**关键设计：配置驱动的 provider 切换（零代码改动）**
```csharp
// 设了 ServiceUrl → LocalStack（或任何 S3 兼容存储）
if (!string.IsNullOrEmpty(serviceUrl))
    _client = new AmazonS3Client(accessKey, secretKey, new AmazonS3Config {
        ServiceURL = serviceUrl, ForcePathStyle = true, UseHttp = ...
    });
else
    _client = new AmazonS3Client(RegionEndpoint.GetBySystemName(region));  // 真实 AWS
```
生产 = 不设 `Aws:S3:ServiceUrl`，走默认凭证链（IAM 角色 / 环境变量）。同一份代码，不同配置。

**只有端到端测试才抓到的两个真实 bug：**

| 坑 | 原因 | 解决方案 |
|---|---|---|
| **首次上传 `NoSuchBucket`** | LocalStack 3.x **不会**根据 `BUCKETS` 环境变量自动建桶（旧文档说会） | 加 `localstack/init-s3-bucket.sh` 挂载到 `/etc/localstack/init/ready.d/`——LocalStack 就绪后跑 `awslocal s3 mb` |
| **Presigned PUT URL 用了 `https://`**，但 LocalStack 跑明文 HTTP → curl exit 60（SSL 错误） | AWS SDK 默认在 presigned URL 里用 `https`，对 URL 字符串忽略 `UseHttp` | 检测 `ServiceUrl` 以 `http://` 开头，对生成的 URL 后处理：`url.Replace("https://", "http://")` |

**教训：** Presigned URL 由 SDK *本地*计算（不走网络），所以可以不依赖运行中的 S3/LocalStack 做单元测试——但 **scheme/host 的正确性**只有在真实往返里才暴露。单元测试通过没抓到 https 问题；只有对 URL 跑 `curl PUT` 才抓到。这就是为什么 I/O 密集型功能即使单元测试全绿也要做端到端验证。

#### 4. 贯穿性：「测试通过」不等于证明什么

这次最大的教训：**130/130 单元测试通过，两个 S3 bug 一个都没抓到。** 单元测试验证了 presign 的*逻辑*（key 生成、配置解析）但没验证*线上格式*（https vs http、桶是否存在）。LocalStack 建桶和 URL scheme 的问题，只有在真实 stack 里对真实 presigned URL 跑 `curl PUT` 时才暴露。

**结论：** 对涉及外部系统的功能（S3、SMTP、webhook、支付网关），单元测试全绿是必要但不充分的。在宣布"完成"之前，至少做一次完整的端到端往返（presign → PUT → GET）对着真实（或 LocalStack 模拟的）服务。

### 2026-07-16：PE-1 ~ PE-7 — 微服务 → 订单分片

> **Planned Enhancements** 实施记录（PE-1 ~ PE-7）：技术栈、踩坑与修复。任务清单：[TODO.md](TODO.md)、[HANDOFF.md §11](HANDOFF.md#11-planned-enhancements-scaling-tail--not-scheduled)。设计文档：[MICROSERVICES-PE1.md](docs/MICROSERVICES-PE1.md)、[PE3-ELASTICSEARCH.md](docs/PE3-ELASTICSEARCH.md)、[PE4-PRODUCTION-HARDENING.md](docs/PE4-PRODUCTION-HARDENING.md)、[PE6-REDIS-CART.md](docs/PE6-REDIS-CART.md)、[PE7-SQL-SHARDING.md](docs/PE7-SQL-SHARDING.md)。

#### 技术栈一览（PE-1 ~ PE-7）

| PE | 范围 | 技术栈 |
|---|---|---|
| **PE-1** | 微服务（默认 `docker compose up`） | **Aspire** · **YARP** · **Polly** + **Refit** · **MassTransit** + **RabbitMQ** · **Outbox** · **Saga** · 4 逻辑库 · **OpenTelemetry** + **Jaeger** |
| **PE-2** | 异步消息 | 并入 PE-1 |
| **PE-3** | 全文搜索 | **Elasticsearch 8.15** · Product API；Postgres 回退 |
| **PE-4** | 库存加固 | **Redis** Redlock · **预占库存**（TTL）· **原子 SQL** 扣减 · **YARP 限流** · Redis HA 文档 · **OTel** 库存指标 |
| **PE-5** | 异步结账 + 运维 | PE-1 Saga + **Admin Saga/DLQ 重试 UI** |
| **PE-6** | 购物车缓存 | **Redis** 快照 · Postgres 为真相源 · write-through · 默认 `CartRedis.Enabled=false` |
| **PE-7** | 订单 SQL 分片 | **UserId FNV-1a hash** · `novacart_commerce_0/1` · `order_shard_routes` · 默认 `OrderSharding.Enabled=false` |

#### 0. 流程与验证类错误（贯穿 PE-1 ~ PE-7）

| 错误 | 技术 / 场景 | 修复 / 教训 |
|---|---|---|
| **未经用户要求代其 commit** | Git 流程 | 仅用户明确要求时才 commit；保留改动可用 `git reset HEAD~1` |
| **以「本机无 .NET 8」为由跳过 Docker 验证** | HANDOFF §2 | 项目标准是 `docker compose down && docker compose up --build -d` + `db-migrate` + 冒烟测试，不是宿主机 `dotnet run` |
| **把 exit code 139 当成 SIGSEGV** | Linux 容器里的 .NET | 先看容器日志。本项目里 139 是**未捕获的启动异常**（EF 模型 + DI），不是段错误 |
| **代码修完后仍用旧镜像** | Docker 层缓存 | EF/DI 变更后必须 `docker compose up --build -d`；旧 `auth-api` 镜像仍缺 Saga 主键配置 |

#### 1. PE-1 / PE-2 / PE-5 — 微服务、消息、Saga

**做了什么：** 单体拆为 Auth / Product / Cart / Order，经 YARP 统一入口；结账用 MassTransit Saga + Outbox；邮件走 Order 服务的 MassTransit 消费者；database-per-service（4 个 PostgreSQL 逻辑库）。

**踩坑：**

| 坑 | 原因 | 解决方案 |
|---|---|---|
| **`db-migrate` 失败** | 多个 DbContext 时未指定 `--context` | `migrate-databases.sh` 使用 `--context AppDbContext` + `AppDbContextFactory` |
| **Saga 持久化报错** — `OrderCheckoutState` 无主键 | MassTransit EF Saga 实体未配置 | `AppDbContext`：`HasKey(x => x.CorrelationId)` |
| **迁移 startup 项目校验失败** | 用 `Order.Api` 作 startup 会触发全量 `ValidateOnBuild` | 迁移容器只用 **Core + ServiceDefaults**，不启动完整 API |
| **`cart-api` / `order-api` 启动崩溃** — 无法解析 `IPricingService` | `IPricingService` 只在 `AddNovacartProduct()` 注册 | 移到**共享基础设施** `AddNovacartInfrastructure()` |
| **`Dockerfile.migrate` 构建失败** | 缺少项目引用 | 复制 `Novacart.ServiceDefaults` 到 migrate 镜像 |
| **Windows 主机端口 9200 / 16686 冲突** | 本机已占用 ES 或 Jaeger 端口 | `docker-compose.yml` 去掉 ES/Jaeger 的**主机端口映射**，仅容器内网访问 |
| **旧单体容器仍在跑** | 拆分前 compose 的 `novacart-backend-1` | `docker compose down --remove-orphans` |

**经验教训：**
- **跨服务共用的 DI 必须放在共享扩展里** — Cart/Order 需要的（定价、锁、Outbox 等）进 `AddNovacartInfrastructure`，不要只挂在一个服务扩展上。
- **Saga 实体必须显式配置 EF 主键** — MassTransit 不会自动把 `CorrelationId` 当 PK。
- **迁移工具链要最小化** — 专用 migrate 镜像避免把整棵服务 DI 图拉进设计时校验。
- **PE-2、PE-5 不是独立里程碑** — 跟 PE-1 一起交付；RabbitMQ + Saga + Outbox 同一波上线。
- **跨服务 DI 或 EF 模型变更后，必须验证完整 compose**，不能只看单元测试。

#### 2. PE-3 — ElasticSearch 商品搜索

**技术栈：** Elasticsearch 8.15 · `Elastic.Clients.Elasticsearch` 8.15 · 索引 `novacart-products` · Product API · `PagedResult.searchEngine` 可选字段。

**做了什么：** 有关键词（`?q=`）且 ES 健康时走 ES；Admin/Square 变更触发索引；启动 reindex；失败自动回退 Postgres。

**踩坑：**

| 坑 | 原因 | 解决方案 |
|---|---|---|
| **C# 客户端编译错误** | ES 8.x 客户端 API 与旧 NEST 不同 | 使用 `Elastic.Clients.Elasticsearch`：`.Term(new TermsQueryField(...))`、`NumberRange`、`.Match()` 的 union 类型 |
| **Indexer 与 DbContext 生命周期** | Singleton Indexer 持有 Scoped DbContext | `ElasticProductSearchIndexer` 注册为 **Scoped**（或通过 scope factory 解析） |
| **InMemory EF 无法测 ILIKE 回退** | InMemory 不支持 Postgres `ILIKE` | ES 路径用 Testcontainers；回退路径在集成/Docker 测，不用 InMemory |
| **测试容器内再跑 Testcontainers** | 嵌套 Docker / 无 socket | 懒加载容器；无 Docker 时 skip |
| **以为 ES 必须映射主机 9200** | 开发机常已占用 9200 | ES 仅在 Docker 网络内；网关/product-api 访问 `http://elasticsearch:9200` |

**经验教训：**
- **无关键词的浏览仍走 Postgres** — ES 负责关键词相关性；facet/sort 逻辑不变。
- **必须实现回退并在响应里标明** — `searchEngine: "postgres"` 便于运维判断走了哪条路径。
- **新客户端要读官方文档** — `Elastic.Clients.Elasticsearch` 不是 NEST 的直接替换。
- 配置与同步表见 [docs/PE3-ELASTICSEARCH.md](docs/PE3-ELASTICSEARCH.md)。

#### 3. PE-4 — 分布式锁与库存加固 ✅

**技术栈：** Redis 7 · `RedisDistributedLockService` · `StockHoldService` + `stock_holds` · `ProductStockRepository` 条件 UPDATE · YARP 限流 · [PE4-REDIS-HA.md](docs/PE4-REDIS-HA.md) · OTel `Novacart.Stock`。

**已完成：** Redlock 基线 + 生产加固 — checkout 预占、TTL 释放、原子 SQL 扣减、网关限流、Redis HA 配置文档、库存/锁指标。

**踩坑 / 误解：**

| 坑 | 原因 | 解决方案 |
|---|---|---|
| **单元测试假锁永远成功** | Mock 无法模拟竞争 | Testcontainers Redis |
| **「有 Redis 锁 = 生产级库存」** | 锁 alone 不够 | PE-4 加固已补预占 + 原子 SQL + 限流（✅） |
| **PE-6 与 PE-4 混为一谈** | 购物车缓存 vs 库存锁 | PE-6 读优化；PE-4 写保护 — 不同边界 |
| **购物车 Redis 与库存 Redis** | 同一 Redis 实例 | key 前缀分离：`novacart:cart:*` vs `novacart:stock:*` |

**Spring Cloud 大型商城 — 当前对照**

| 能力 | Spring 常见 | Novacart 现状 |
|---|---|---|
| 扣库存锁 | Redisson | ✅ Redlock + HA 文档 |
| 预占库存 | hold + TTL | ✅ `StockHoldService` |
| DB 条件更新 | 条件 `UPDATE` | ✅ `ProductStockRepository` |
| 秒杀限流 | Sentinel | ✅ YARP 固定窗口 |
| 购物车 | Redis | ✅ PE-6（Postgres 真相源） |
| 订单分片 | ShardingSphere | ✅ PE-7 UserId hash 试点 |

**结论：** 详见 [docs/PE4-PRODUCTION-HARDENING.md](docs/PE4-PRODUCTION-HARDENING.md)。

#### 4. PE-5 — Admin Saga 可见性 ✅

**做了什么：** `CheckoutSagaAdminService` 列表/重试；DLQ retry；Admin `/admin/system` 页面。

**教训：** PE-5 核心结账在 PE-1 Saga；**Admin 重试 UI** 是 PE-5 独立交付物。

#### 5. PE-6 — Redis 购物车缓存 ✅

**技术栈：** `CartRedisStore` · `CartRedisSnapshot` · `CartRedisOptions`。

**做了什么：** 读缓存 / 写 PG + write-through；价格库存仍走 catalog；clear/merge/checkout 时删 key。

**踩坑：**

| 坑 | 原因 | 解决方案 |
|---|---|---|
| **`Cart` 命名空间与实体冲突** | DTO 命名空间 vs 实体类 | 别名 `CartEntity = …Entities.Cart`；**禁止**对 `CartService.cs` 做 replace_all |
| **在 Redis 里缓存价格** | 价格规则会变 | 只缓存 line items；DTO 构建时查 catalog |

**结论：** [docs/PE6-REDIS-CART.md](docs/PE6-REDIS-CART.md)。默认 `CartRedis.Enabled=false`。**后续 ✅：** `CartRedisIntegrationTests`（Testcontainers Redis）。

#### 6. PE-7 — 订单 SQL 分片试点 ✅

**技术栈：** `OrderShardResolver`（FNV-1a）· `order_shard_routes` · `IShardedOrderDb` · `novacart_commerce_0/1`。

**做了什么：** 按 UserId 路由 orders/items/payments；路由表在 legacy commerce DB；Admin fan-out；webhook 按 orderId 查路由。

**踩坑：**

| 坑 | 原因 | 解决方案 |
|---|---|---|
| **`Guid.GetHashCode()` 做分片** | 不稳定 / 可能为负 | FNV-1a + 无符号取模 |
| **CartService replace_all** | 破坏 DTO/接口名 | 仅实体用别名 |
| **Admin 跨分片分页** | 无法单库 ORDER BY | fan-out + 内存合并（试点够用） |
| **Backfill 后 Analytics 双计** | legacy 与 shard 重复 | legacy 查询排除 `order_shard_routes` 已有订单 |
| **Backfill 误写** | 直接改 prod | CLI 默认 dry-run；`--apply` / `--delete-legacy` 显式 |

**可选后续（2026-07-16）✅：** `AnalyticsService` 跨分片聚合 · `OrderShardBackfillService` + `scripts/backfill-order-shards.sh` · `OrderShardingIntegrationTests`。

**结论：** [docs/PE7-SQL-SHARDING.md](docs/PE7-SQL-SHARDING.md)。默认 `OrderSharding.Enabled=false`。

#### 7. 「测试通过」仍证明不了什么（PE-1 ~ PE-7）

与 S3 那次同一主题：

- **单元测试全绿**没抓到 Cart/Order 启动 DI 失败 — 只有 **完整 compose up** 才暴露。
- **InMemory EF** 没验证 Postgres `ILIKE` 回退或真实 ES 查询。
- **Mock 锁**没证明并发 — **Testcontainers Redis** 才证明。
- **PE-6/PE-7 启用路径** — `CartRedisIntegrationTests` / `OrderShardingIntegrationTests` 已覆盖；staging 仍建议 compose 开 flag 做验收。
- **HANDOFF 冒烟**（`GET /api/products?q=…` → `searchEngine: "elasticsearch"`）是 PE-3 验收标准。

**经验法则：** 涉及**多容器**、**外部基础设施**（ES、Redis、RabbitMQ）、**共享 DI** 或 **多库路由** 的 PE，完成后跑 `docker compose up --build -d`、确认 `db-migrate` exit 0，再经网关打接口 — 宿主机无 .NET 8 时用 `Dockerfile.backend.test`。

---
> **⚠️ 重要：使用任何技术之前，务必先阅读官方文档。**
> 不需要读完全部文档 — 先看 Quick Start / Tutorial，跑通最小示例，然后边写边查。官方文档是唯一真实来源。

---

## 官方文档链接

### 必读（核心依赖）

| 技术 | 官方文档 | 先看什么 |
|---|---|---|
| **ASP.NET Core** | [learn.microsoft.com/aspnet/core](https://learn.microsoft.com/en-us/aspnet/core/) | 基础 → Middleware → DI → MVC |
| **Entity Framework Core** | [learn.microsoft.com/ef/core](https://learn.microsoft.com/en-us/ef/core/) | 入门 → DbContext → Migrations → 查询 |
| **Next.js** | [nextjs.org/docs](https://nextjs.org/docs) | App Router → Server Components → API Routes → Middleware |
| **TypeScript** | [typescriptlang.org/docs](https://www.typescriptlang.org/docs/) | 基本类型 → 接口 → 泛型 |
| **Stripe** | [stripe.com/docs](https://stripe.com/docs) | Payment Intents → Webhooks → 测试 |
| **PostgreSQL** | [postgresql.org/docs](https://www.postgresql.org/docs/) | SQL 语法 → 数据类型 → 索引 |
| **Tailwind CSS** | [tailwindcss.com/docs](https://tailwindcss.com/docs) | Utility Classes → 响应式 → 配置 |

### 建议读（重要支撑技术）

| 技术 | 官方文档 | 先看什么 |
|---|---|---|
| **Redis** | [redis.io/docs](https://redis.io/docs/) | 数据类型 → 命令 → Pub/Sub |
| **Square API** | [developer.squareup.com/docs](https://developer.squareup.com/docs/) | Catalogue API → Sandbox → OAuth |
| **Docker** | [docs.docker.com](https://docs.docker.com/) | 入门 → Dockerfile → Compose |
| **AWS** | [docs.aws.amazon.com](https://docs.aws.amazon.com/) | EC2 → RDS → ElastiCache → S3 |
| **GitHub Actions** | [docs.github.com/actions](https://docs.github.com/en/actions) | Workflow 语法 → Jobs → Triggers |
| **Swashbuckle (Swagger)** | [github.com/.../Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore) | README → Getting Started |

### 速查（用到时再看）

| 技术 | 官方文档 |
|---|---|
| **ngrok** | [ngrok.com/docs](https://ngrok.com/docs/) |
| **xUnit** | [xunit.net/docs](https://xunit.net/docs/) |
| **Vitest** | [vitest.dev](https://vitest.dev/) |
| **React Testing Library** | [testing-library.com/docs/react](https://testing-library.com/docs/react-testing-library/intro/) |
| **ECharts** | [echarts.apache.org/en](https://echarts.apache.org/en/index.html) |
| **Zustand** | [zustand.docs.pmnd.rs](https://zustand.docs.pmnd.rs/getting-started/introduction) |
| **Polly** | [github.com/App-vNext/Polly](https://github.com/App-vNext/Polly) |
| **MassTransit** | [masstransit.io](https://masstransit.io/documentation/getting-started) |
| **Consul** | [developer.hashicorp.com/consul](https://developer.hashicorp.com/consul/docs) |
| **YARP** | [microsoft.github.io/reverse-proxy](https://microsoft.github.io/reverse-proxy/articles/getting-started.html) |
| **Ocelot** | [github.com/ThreeMammals/Ocelot](https://github.com/ThreeMammals/Ocelot) |
| **OpenTelemetry** | [opentelemetry.io/docs](https://opentelemetry.io/docs/) |
| **RabbitMQ** | [rabbitmq.com/docs](https://www.rabbitmq.com/docs) |
| **Kafka** | [kafka.apache.org/documentation](https://kafka.apache.org/documentation/) |
| **ElasticSearch** | [elastic.co/guide](https://www.elastic.co/guide/index.html) |

---

## 架构决策

### 单体 MVP vs 微服务

| 维度 | 单体 MVP | 微服务 |
|---|---|---|
| 项目数量 | 1 个应用 | 5+ 个独立服务 |
| 数据库 | 1 个共享数据库 | 每个服务独立数据库 |
| 部署 | 1 个 Docker 容器 | N 个容器 + 网关 + 服务发现 |
| 开发周期 | 几周 | 几个月 |
| 团队规模 | 1-2 人 | 5+ 人 |
| 适用场景 | MVP、毕业设计 | 大型生产系统 |

**决策：从单体 MVP 开始（MonolithFirst 模式）**

- Martin Fowler 的建议：先构建单体，后续按需拆分
- 为未来拆分做准备的关键：
  - 清晰的分层架构（Controller → Service → Mapper）
  - 模块边界清晰（Auth、Product、Cart、Order 代码分离）
  - 按模块划分数据库表（避免跨模块 JOIN）
  - RESTful API 设计（拆分时前端无需改动）

### 分层架构

```
Controller（HTTP）→ Service（业务逻辑）→ Mapper/Repository（数据访问）→ Entity（领域模型）
```

- **Controller**：处理 HTTP 请求，校验输入，返回响应
- **Service**：包含业务逻辑，编排操作
- **Mapper/Repository**：数据访问层，SQL 查询，ORM 操作
- **Entity**：领域模型，数据库表映射

---

## 后端技术

### ASP.NET Core vs Spring Boot

| 维度 | ASP.NET Core（.NET） | Spring Boot（Java） |
|---|---|---|
| 语言 | C# | Java |
| ORM | Entity Framework Core | MyBatis Plus / JPA |
| 依赖注入 | 内置 | Spring IoC 容器 |
| 性能 | 优秀（Kestrel） | 优秀（Tomcat/Netty） |
| 跨平台 | 是（.NET 8+） | 是（JVM） |

### Entity Framework Core（EF Core）

.NET 官方 ORM，等价于 Java 生态中的 MyBatis Plus。

| MyBatis Plus（Java） | EF Core（.NET） |
|---|---|
| SQL 映射，XML/注解 | Code-First，从 C# 类自动生成表结构 |
| `BaseMapper<T>` 提供 CRUD | `DbContext` + LINQ 提供 CRUD |
| Lambda QueryWrapper | LINQ 查询表达式 |
| 代码生成器 | EF Core Migrations |
| 复杂查询需手写 SQL | 大多数场景 LINQ 够用，也支持原生 SQL |

**核心概念：**
- **Code-First**：定义 C# 类 → EF 自动生成数据库结构
- **Migrations**：数据库结构变更的版本控制（`dotnet ef migrations add`、`dotnet ef database update`）
- **DbContext**：数据库交互的主类，等价于 MyBatis 的 Mapper 接口

### Swagger / OpenAPI

自动生成交互式 API 文档。

- 从控制器注解/装饰器自动生成
- 前端开发者无需手动文档即可探索、测试和集成 API
- 契约开发的唯一真实来源
- ASP.NET Core：Swashbuckle NuGet 包

### 统一 API 层（前端）

基于 Fetch API 的可复用 `apiCall` 封装：
- 自动向请求头注入 JWT Token
- 解析 3 种错误格式（JSON、文本、网络）
- 401/403 响应触发自动登出
- 消除 22+ 个前端 API 调用的样板代码

---

## 前端技术

### Next.js vs CRA vs Vite

| 工具 | 状态 | 用途 |
|---|---|---|
| **Next.js** | ✅ 当前标准 | SSR/SSG、全栈 React 应用 |
| **CRA** | ❌ 已弃用 | 仅遗留项目 |
| **Vite** | ✅ 现代工具 | SPA、库开发 |

**Next.js 优势：**
- 内置 App Router（无需 React Router）
- 服务端渲染（SSR）和静态生成（SSG）
- API Routes（同项目内写后端端点）
- 内置 Turbopack/Webpack（无需 Vite）
- 开箱即用的图片优化、代码分割

### React 虚拟 DOM 与 Diff 算法

**React 为什么快：**
1. 维护真实 DOM 的轻量副本（虚拟 DOM）
2. 状态变化时，创建新的虚拟 DOM 树
3. 将新树与旧树进行 Diff 比较（Reconciliation）
4. 仅更新实际变化的 DOM 节点
5. 批量更新，最小化重排/重绘

### Custom Hooks vs HOC（高阶组件）

| 维度 | Custom Hooks | HOC |
|---|---|---|
| 组合性 | 清晰、可组合 | 嵌套地狱 |
| TypeScript | 优秀的类型推断 | 复杂的泛型类型 |
| 命名 | `useAuth`、`useCart` | `withAuth(Component)` |
| React 推荐 | ✅ 首选 | ⚠️ 遗留模式 |
| 调试 | 清晰的组件树 | 包裹层级混乱 |

**Custom Hooks 示例：**
- `useAuth()` — 认证状态和方法
- `useCart()` — 购物车操作（添加、删除、更新）
- `useApi()` — API 调用，含 loading/error 状态

### 可复用组件设计

提取共享组件减少代码重复（约 40%）：
- **布局组件**：Sidebar、Header、Footer
- **业务组件**：ProductCard、CartItem、OrderRow
- **UI 组件**：Button、Input、Modal、Toast

---

## 数据库与缓存

### PostgreSQL vs MySQL

两者都是关系型数据库，PostgreSQL 的优势：
- 更好的 JSON 支持（JSONB）
- 更高级的索引类型（GIN、GiST）
- 更严格的 ACID 合规性
- 更好的并发处理（MVCC）

### Redis 在电商中的应用场景

| 场景 | 说明 |
|---|---|
| 会话存储 | 存储用户会话，快速检索 |
| 购物车缓存 | 购物车数据，亚毫秒级读取 |
| 订单缓存 | 近期订单，避免重复数据库查询 |
| JWT 黑名单 | 已失效的 Token（过期前登出） |
| 限流 | 按 IP/用户统计请求数 |
| 分布式锁 | Redlock 实现库存扣减原子性 |

### 阿里巴巴开发规范（数据库）

- **索引命名**：`idx_表名_字段名`（如 `idx_order_user_id`）
- **字段类型**：ID 用 `BIGINT`，金额用 `DECIMAL`（绝不用 FLOAT）
- **SQL 规范**：避免 `SELECT *`，使用明确的列名
- **命名规范**：表名和列名使用 snake_case

### SQL 分表（规划中）

大表水平分区：
- **按时间**：订单表按月/年拆分
- **按用户 ID**：基于哈希的分区
- **使用时机**：单表 > 1000 万行，查询性能下降

---

## 认证与安全

### JWT（JSON Web Token）实现

**结构：** `Header.Payload.Signature`

**细节：**
- **算法**：HS256（HMAC-SHA256），使用服务端密钥
- **载荷**：用户 ID、邮箱、角色声明
- **过期**：短期 access token（默认 15 分钟）+ 长期 refresh token（7 天）——见下方 Refresh Token
- **提取**：`Authorization: Bearer <token>` 请求头，或通过 `OnMessageReceived` 自动从 `novacart_jwt` HttpOnly cookie 读取

### Token 存储安全

| 方式 | 安全性 | 推荐 |
|---|---|---|
| **HttpOnly Cookie** | ✅ JS 无法访问，防止 XSS | **推荐** |
| **内存（React State）** | ✅ 页面刷新丢失，最安全 | 配合 Refresh Token |
| **localStorage** | ❌ XSS 可直接读取 | **不推荐** |
| **sessionStorage** | ❌ XSS 可直接读取 | **不推荐** |

**HttpOnly Cookie 标志：**
- `HttpOnly` — JavaScript 无法访问
- `Secure` — 生产环境仅 HTTPS
- `SameSite=Strict` — 防止 CSRF 攻击

### Refresh Token（Access + Refresh 双 Token）

短期 access token 限制了 token 被盗后的影响范围,但会迫使频繁重新认证。refresh token 模式在保持安全的同时让体验顺滑。

**双 token 模型：**
| Token | 寿命 | 存于 | 用途 |
|---|---|---|---|
| **Access token**（JWT） | 15 分钟 | `novacart_jwt` cookie，`Path=/api` | 为每个 API 请求授权 |
| **Refresh token**（opaque，不透明） | 7 天 | `novacart_refresh` cookie，`Path=/api/auth` | 通过 `POST /api/auth/refresh` 换取新 access token |

**关键实践：**
- **Refresh token 不透明 + 哈希存储。** 数据库只存 SHA-256 哈希（`refresh_tokens` 表）；原始值只返回给 cookie 一次。和密码不同,不需要慢速 KDF（bcrypt）——token 本身已是 256 位随机。
- **每次 refresh 都轮换。** 每次 refresh 撤销旧 token 并签发新的一对（`ReplacedByTokenHash` 链接它们）。一个 token 只能用一次。
- **重用检测。** 如果一个*已撤销*的 token 再次出现,假设被盗并撤销整个家族（该用户的所有 token）。这是轮换的主要安全收益。
- **收窄 refresh cookie 路径。** `Path=/api/auth` 意味着 refresh cookie 只发给 auth 端点——绝不随商品/购物车/订单请求发送,最小化暴露。
- **前端合并。** access token 过期时,多个并发请求同时 401。模块级单例 `refreshPromise` 把它们合并成一次 refresh 调用;否则 N 个并行 refresh 会各自轮换、互相失效。

### 基于角色的访问控制（RBAC）

- 三种角色：顾客、管理员、系统管理员
- 角色声明嵌入 JWT Token
- 前端：路由守卫重定向未授权用户
- 后端：端点授权拒绝非管理员 Token（403 Forbidden）

### 密码安全

- **哈希**：bcrypt（加盐、单向哈希）
- **密码重置流程**：请求 → 生成限时 Token → 邮件 → 提交新密码和 Token → 验证并更新

### 表单验证

- 客户端实时验证
- 动态按钮状态（表单无效时禁用）
- Enter 键提交支持
- 服务端验证作为第二层防线

---

## 消息队列与异步处理

### RabbitMQ vs Kafka vs RocketMQ

| 特性 | RabbitMQ | Kafka | RocketMQ |
|---|---|---|---|
| 模型 | 队列模式 | 日志模式 | 队列模式 |
| 吞吐量 | 中等 | 极高 | 高 |
| 用途 | 任务队列、RPC | 事件流、日志 | 电商（阿里巴巴） |
| 复杂度 | 低 | 中等 | 中等 |
| .NET 支持 | ✅ 官方客户端 | ✅ Confluent 客户端 | ⚠️ 有限 |

**RabbitMQ 在电商中的应用：**
- 异步订单处理（支付 → 库存 → 邮件）
- 邮件通知队列
- 库存更新队列

### 何时使用消息队列

- ✅ 结账后多个步骤（支付、库存、邮件、仓库）
- ✅ 高并发流量（秒杀）
- ✅ 微服务间通信
- ❌ 简单同步操作（MVP 阶段）
- ❌ 单体架构

### 使用 `Channel<T>` 的进程内队列（Novacart 实际所用）

在引入真正的 broker（RabbitMQ）之前,Novacart 使用 .NET 内置的 `System.Threading.Channels`——一个轻量级进程内生产者/消费者队列——把慢速副作用（邮件）从请求处理中解耦。

**如何运作：**
```
请求处理 ──入队──▶ Channel<EmailMessage>（有界）──▶ BackgroundService worker ──▶ SMTP
（立即返回）          （背压）                     （每条消息独立 DI scope）
```

**`Channel<T>` vs RabbitMQ —— 何时用哪个：**
| | `Channel<T>`（进程内） | RabbitMQ（broker） |
|---|---|---|
| 作用范围 | 单进程 | 跨进程 / 跨服务 |
| 持久化 | ❌ 重启丢失 | ✅ 重启存活（持久化队列） |
| 搭建成本 | 零（.NET 内置） | 需基础设施（容器 + 配置） |
| 背压 | ✅ `BoundedChannelFullMode.Wait` | ✅ QoS / prefetch |
| 适用场景 | 单体内部解耦 | 跨服务分发工作 |

**Novacart 的选择：** 邮件队列是进程内的,因为 Novacart 是单体——同一进程内只有一个生产者和一个消费者。如果以后拆分成服务,把 `Channel<T>` 换成 RabbitMQ 消费者是局部改动。这次暴露的 scope 生命周期坑见[实战记录](#2026-0715p2p3-功能--jwt-refresh异步邮件s3以及它们暴露出来的-bug)。

---

## 搜索引擎

### ElasticSearch

全文搜索引擎，补充 SQL 数据库。

**适用场景：**
- 商品目录 > 10,000 件
- 复杂多面搜索（材质、风格、价格区间）
- 跨字段全文关键词搜索
- SQL `LIKE` 查询性能不足

**不需要的场景：**
- 商品数据来自外部 API（Square）
- 数据量小（< 1,000 件）
- 简单筛选即可满足需求

---

## 设计模式

### 策略模式（Strategy Pattern）

**用途**：可互换的支付提供商（Stripe、PayPal、未来网关）

```
IPaymentStrategy（接口）
├── StripePaymentStrategy
├── PayPalPaymentStrategy
└── AlipayPaymentStrategy

PaymentContext → 使用 IPaymentStrategy
```

- 开闭原则：新增支付方式无需修改现有代码
- 客户端代码依赖接口，而非具体实现

### 工厂模式（Factory Pattern）

**用途**：对象创建逻辑（支付策略创建、通知创建）

```
PaymentFactory.Create("stripe") → StripePaymentStrategy
PaymentFactory.Create("paypal") → PayPalPaymentStrategy
```

### 其他常用模式

| 模式 | 用途 |
|---|---|
| 单例模式（Singleton） | 数据库连接、配置 |
| 观察者模式（Observer） | 事件驱动更新 |
| 建造者模式（Builder） | 复杂对象构建（如查询构建器） |
| 仓储模式（Repository） | 数据访问抽象 |

---

## 测试

### 后端：xUnit（.NET）

- .NET 行业标准测试框架
- 单元测试：测试单个方法/函数
- 集成测试：端到端测试 API 端点
- 每次提交在 CI 流水线中自动执行

```csharp
[Fact]
public void CalculateTotal_ReturnsCorrectSum()
{
    var cart = new Cart { Items = new[] { new Item { Price = 10, Qty = 2 } } };
    Assert.Equal(20, cart.CalculateTotal());
}
```

### 前端：Vitest + React Testing Library

| 工具 | 用途 |
|---|---|
| **Vitest** | 原生 ESM 支持的快速单元测试 |
| **React Testing Library** | 从用户视角进行组件测试 |

```typescript
// Vitest
import { describe, it, expect } from 'vitest';
describe('Cart', () => {
  it('calculates total', () => {
    expect(calculateTotal([{price: 10, qty: 2}])).toBe(20);
  });
});

// React Testing Library
import { render, screen } from '@testing-library/react';
render(<ProductCard name="Test" price={10} />);
expect(screen.getByText('Test')).toBeInTheDocument();
```

---

## DevOps 与部署

### Docker

```yaml
# docker-compose.yml
services:
  frontend:
    build: ./frontend
    ports: ["3000:3000"]
  backend:
    build: ./backend
    ports: ["5000:5000"]
  postgres:
    image: postgres:16
    ports: ["5432:5432"]
  redis:
    image: redis:7
    ports: ["6379:6379"]
```

### AWS 架构

```
用户 → EC2（应用）→ RDS（PostgreSQL）
                   → ElastiCache（Redis）
                   → S3（静态资源）
```

### GitHub Actions CI/CD

- Push 时自动构建
- 运行测试（xUnit + Vitest）
- Docker 镜像构建和推送
- 部署到 AWS

---

## 性能优化

### Intersection Observer（懒加载）

```javascript
// 图片懒加载 — 仅在进入视口时加载
const observer = new IntersectionObserver((entries) => {
  entries.forEach(entry => {
    if (entry.isIntersecting) {
      entry.target.src = entry.target.dataset.src;
      observer.unobserve(entry.target);
    }
  });
});

document.querySelectorAll('img[data-src]').forEach(img => observer.observe(img));
```

**好处：**
- 减少首屏加载时间
- 节省带宽（折叠下方的图片不加载）
- 改善 Core Web Vitals（LCP、FID）

### 线程池调优

- ASP.NET Core 使用 `Task` 和内置线程池
- 通过 `ThreadPool.SetMinThreads` 自定义配置，应对高并发场景
- 用途：秒杀、批量订单处理

### 代码分割

- Next.js App Router：按路由自动代码分割
- 动态导入：`const Component = dynamic(() => import('./Component'))`
- 减少初始包大小

---

## CSS 与响应式设计

### CSS Grid 自适应布局

```css
.product-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);  /* 桌面端 */
  gap: var(--section-gap);
}

@media (max-width: 1200px) {
  .product-grid { grid-template-columns: repeat(3, 1fr); }  /* 平板横屏 */
}

@media (max-width: 900px) {
  .product-grid { grid-template-columns: repeat(2, 1fr); }  /* 平板竖屏 */
}

@media (max-width: 600px) {
  .product-grid { grid-template-columns: 1fr; }  /* 移动端 */
}
```

### CSS 自定义属性

```css
:root {
  --container-max: 1200px;
  --section-gap: 1.5rem;
  --header-height: 4rem;
}
```

### rem vs px vs em

| 单位 | 说明 | 用途 |
|---|---|---|
| **rem** | 相对于根元素字号（默认 16px） | ✅ 字号、间距 |
| **px** | 绝对像素 | ❌ 响应式避免使用 |
| **em** | 相对于父元素字号 | 组件级缩放 |

### 响应式侧边栏（自动折叠）

```javascript
// ≤768px 时自动折叠侧边栏
const mediaQuery = window.matchMedia('(max-width: 768px)');
mediaQuery.addEventListener('change', (e) => {
  setSidebarCollapsed(e.matches);
});
```

---

## PWA（渐进式 Web 应用）

### 核心组件

| 组件 | 文件 | 用途 |
|---|---|---|
| **Web App Manifest** | `manifest.json` | 应用名称、图标、主题色、显示模式 |
| **Service Worker** | `sw.js` | 离线缓存、后台同步、推送通知 |
| **可安装** | 浏览器提示 | 添加到主屏幕 |

### Service Worker 生命周期

```
注册 → 安装 → 激活 → Fetch（拦截网络请求）
```

### manifest.json 示例

```json
{
  "name": "Novacart",
  "short_name": "Novacart",
  "start_url": "/",
  "display": "standalone",
  "background_colour": "#ffffff",
  "theme_colour": "#000000",
  "icons": [
    { "src": "/icon-192.png", "sizes": "192x192", "type": "image/png" },
    { "src": "/icon-512.png", "sizes": "512x512", "type": "image/png" }
  ]
}
```

---

## 支付集成

### Stripe 沙盒

- **测试模式**：无真实扣款，使用测试卡号
- **测试卡号**：`4242 4242 4242 4242`，任意未来日期，任意 CVC
- **支付流程**：客户端创建 Payment Intent → 服务端确认 → Webhook 通知结果

### Stripe Webhooks

- Stripe 在支付事件发生时向你的端点发送 POST 请求
- 事件类型：`payment_intent.succeeded`、`payment_intent.failed`
- **ngrok**：将本地服务器暴露到公网，用于 Webhook 测试

```bash
# 启动 ngrok 隧道
ngrok http 5000

# 转发 Stripe 事件
stripe listen --forward-to localhost:5000/api/webhooks/stripe
```

### 支付令牌化

- 银行卡信息不经过你的服务器
- Stripe.js 在客户端生成 Token
- 服务端只接收 Token，不接收卡号
- 简化 PCI 合规

---

## 对象存储（S3）

### Presigned URL —— 直传 S3

后端不代理文件上传（那会在内存里缓冲整个文件）,而是签发一个 **presigned URL**,浏览器直接上传到 S3。

**流程：**
```
浏览器 ──POST /api/admin/uploads/presign──▶ 后端 ──▶ { uploadUrl, publicUrl }
浏览器 ──PUT uploadUrl（文件体）────────────▶ S3
浏览器把 publicUrl 存为 product.ImageUrl
```

**为什么用 presigned：**
- 后端从不处理文件字节 → 无内存/CPU 压力
- 上传直达 S3 的 CDN 边缘 → 大文件更快
- URL 会过期（如 5 分钟）→ 复用窗口有限

**配置驱动切换 provider（LocalStack ↔ AWS）：**
```csharp
// 设了 ServiceUrl → LocalStack / S3 兼容（开发,无需 AWS 账号）
// 未设 ServiceUrl → 真实 AWS,走默认凭证链（生产）
```

### LocalStack —— 无需 AWS 账号的 S3

[LocalStack](https://localstack.local/) 在本地模拟 AWS 服务。开发时 `docker-compose` 跑 `localstack/localstack`,这样无需 AWS 凭证或计费就能完整走通 S3 代码路径。

**两个坑（只有端到端测试才发现）：**
1. **LocalStack 3.x 不会自动建桶**,尽管设了 `BUCKETS` 环境变量。修复：把 init 脚本挂载到 `/etc/localstack/init/ready.d/`,跑 `awslocal s3 mb`。
2. **Presigned URL 默认用 `https://`** 但 LocalStack 跑明文 HTTP。修复：当 `ServiceUrl` 是 `http://` 时,对生成的 URL 后处理强制改成 `http://`。

> 完整调试过程见[实战记录](#2026-0715p2p3-功能--jwt-refresh异步邮件s3以及它们暴露出来的-bug)。

---

## 微服务概念

### .NET 微服务对应组件

| Spring Cloud（Java） | .NET 替代方案 |
|---|---|
| Nacos（注册中心） | Consul / .NET Aspire |
| Nacos（配置中心） | .NET Configuration + Consul KV |
| Spring Cloud Gateway | YARP / Ocelot |
| Sentinel（熔断器） | Polly |
| OpenFeign（HTTP 客户端） | HttpClientFactory + Refit |
| Seata（分布式事务） | Saga 模式 + MassTransit |
| Spring Cloud Stream | MassTransit（RabbitMQ/Kafka） |
| Sleuth + Zipkin（链路追踪） | OpenTelemetry + Jaeger |

### API 网关

- **Ocelot**：.NET API 网关（路由、限流、认证）
- **YARP**：微软的反向代理（高性能）
- **用途**：统一入口，隐藏内部服务拓扑

### 服务发现

- **Consul**：HashiCorp 的服务发现 + 配置中心
- **.NET Aspire**：微软的云原生技术栈（较新）

---

## 分布式事务

### ACID vs BASE

| ACID（传统） | BASE（分布式） |
|---|---|
| 原子性（Atomicity） | 基本可用（Basically Available） |
| 一致性（Consistency） | 软状态（Soft state） |
| 隔离性（Isolation） | 最终一致（Eventually consistent） |
| 持久性（Durability） | |

### Seata 模式

| 模式 | 工作原理 | 优点 | 缺点 |
|---|---|---|---|
| **AT** | 自动生成 undo_log，失败时自动回滚 | 简单，无需手写回滚代码 | 性能开销（额外写入） |
| **TCC** | 手动实现 Try/Confirm/Cancel 方法 | 高性能，细粒度控制 | 开发成本高 |
| **Saga** | 每个步骤配一个补偿操作，失败时逆序补偿 | 适合长事务 | 最终一致性，有延迟 |

**AT 模式流程：**
1. 开启全局事务
2. 每个本地事务写入 undo_log（前镜像 + 后镜像）
3. 全部成功 → 提交，删除 undo_log
4. 任意失败 → 执行 undo_log 回滚

**何时需要：** 多服务、多数据库场景。
**单数据库：** 使用本地事务（BEGIN/COMMIT/ROLLBACK）即可。

---

## 面试速查表

### 为什么选 React？

- 虚拟 DOM + Diff 算法 → 仅重新渲染变化的组件
- 组件化架构 → 可复用、可测试、可组合
- 单向数据流 → 可预测的状态管理
- 庞大生态（Next.js、React Native 等）

### 为什么选 Next.js 而非 CRA？

- CRA 已弃用；Next.js 是 React 团队推荐的框架
- 内置 SSR/SSG（更好的 SEO、更快的首屏加载）
- App Router 文件路由（无需 React Router）
- API Routes（同项目写后端）
- 开箱即用的图片优化、代码分割

### 为什么选 PostgreSQL 而非 MySQL？

- 更好的 JSON 支持（JSONB）
- 高级索引（GIN、GiST）
- 更严格的 ACID 合规
- 更好的并发处理（MVCC）

### EF Core vs Dapper

| EF Core | Dapper |
|---|---|
| 全功能 ORM，Code-First | 微型 ORM，SQL 优先 |
| LINQ 查询 | 原生 SQL |
| 内置 Migrations | 手动管理 Schema |
| 较慢（有开销） | 更快（直接 SQL） |
| 功能更多 | 控制更多 |

### JWT 存在哪里？

**HttpOnly Cookie** — 防止 XSS 窃取 Token。绝不用 localStorage/sessionStorage。

### MonolithFirst

先构建单体 → 模块化 → 按需拆分服务。不要一开始就做微服务。

### 策略模式 vs 工厂模式

- **策略模式**：运行时选择算法（支付方式）
- **工厂模式**：创建对象时不指定具体类（支付策略创建）

---

## 术语表

| 术语 | 定义 |
|---|---|
| **JWT** | JSON Web Token — 无状态认证令牌 |
| **RBAC** | 基于角色的访问控制 |
| **ORM** | 对象关系映射（EF Core、MyBatis Plus） |
| **PWA** | 渐进式 Web 应用 — 可安装、支持离线 |
| **SSR** | 服务端渲染 |
| **SSG** | 静态站点生成 |
| **SPA** | 单页应用 |
| **CQRS** | 命令查询职责分离 |
| **ACID** | 原子性、一致性、隔离性、持久性 |
| **MVCC** | 多版本并发控制 |
| **CORS** | 跨域资源共享 |
| **CSRF** | 跨站请求伪造 |
| **XSS** | 跨站脚本攻击 |
| **PCI** | 支付卡行业（合规标准） |
| **Redlock** | Redis 分布式锁算法 |
| **AT 模式** | Seata 中的自动事务模式 |
| **TCC** | Try-Confirm-Cancel 分布式事务模式 |
| **Saga** | 长事务补偿模式 |
