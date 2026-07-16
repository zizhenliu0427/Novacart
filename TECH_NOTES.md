# Novacart — Technical Knowledge Notes

> Comprehensive notes covering all technologies, concepts, and architectural decisions discussed during the project planning phase.
> Targeting Senior Software Engineer skill level for systematic learning.

---

## 📋 Checklist Learning Method (Three-Layer Structure)

```
Layer 1: 10 Domains (Senior Engineer Skills Map)
  └── Layer 2: Specific knowledge points per domain (~315 items, track with checkboxes)
        └── Layer 3: Deep learning per knowledge point (complete 4 steps below)
```

### Layer 3 Study Template

For **each knowledge point** in Layer 2, complete the following 4 steps, then check it off:

- [ ] **① Background & Application Scenarios**
  - What problem does this knowledge point solve?
  - Why is it needed? What happens without it?
  - In what scenarios is it used? (Connect to Novacart project or real work scenarios)

- [ ] **② Alternatives & Easily Confused Concepts**
  - What are the alternatives? Pros and cons of each?
  - What concepts are easily confused? How to distinguish them?
  - Why choose this over others? (Trade-off analysis)

- [ ] **③ Example Code / Practical Operations**
  - Write runnable example code (type it yourself, don't copy-paste)
  - For operational knowledge (Docker, Git), write actual commands and steps
  - Connect to Novacart project, write actual application code snippets

- [ ] **④ 5 Common Interview Questions**
  - Scenario 1: "How do you use XX in your system? Why this design?"
  - Scenario 2: "If XX has a problem, how do you debug and fix it?"
  - Quick Q&A 1: "What's the core principle of XX? One sentence."
  - Quick Q&A 2: "What's the difference between XX and YY?"
  - Comprehensive: "Given a requirement, how would you design it? What factors to consider?"

### Example: "Redis Cache Penetration"

```markdown
## Redis Cache Penetration

### ① Background & Application Scenarios
- **Problem**: Querying non-existent data — cache never hits, every request goes to DB
- **Scenario**: Malicious user repeatedly requests non-existent user_id
- **Impact**: DB pressure spikes, potentially causing service crash

### ② Alternatives & Easily Confused Concepts
- **Cache Penetration** vs **Cache Breakdown** vs **Cache Avalanche**:
  - Penetration: Query non-existent data
  - Breakdown: Hot key expires, many requests hit DB simultaneously
  - Avalanche: Many keys expire at the same time
- **Solution comparison**:
  - Bloom filter: Space-efficient, but has false positives
  - Cache null values: Simple, but wastes memory
  - Input validation: Validate parameters before cache lookup

### ③ Example Code
```csharp
public async Task<User?> GetUserAsync(int userId)
{
    // 1. Input validation (prevent malicious input)
    if (userId <= 0) return null;

    // 2. Check cache
    var cacheKey = $"user:{userId}";
    var cached = await _redis.GetAsync<User>(cacheKey);
    if (cached is not null) return cached;  // Hit (including null marker)

    // 3. Query database
    var user = await _db.Users.FindAsync(userId);

    // 4. Write cache (cache non-existent data too, with short TTL)
    if (user is null)
        await _redis.SetAsync(cacheKey, "NULL", TimeSpan.FromMinutes(5));
    else
        await _redis.SetAsync(cacheKey, user, TimeSpan.FromHours(1));

    return user;
}
```

### ④ 5 Common Interview Questions
1. **Scenario 1**: Your e-commerce product query API is being hit with non-existent IDs, DB CPU spikes to 90%. How do you handle it?
2. **Scenario 2**: You're caching null values but Redis memory is growing fast. How to optimise?
3. **Quick Q&A 1**: What's the core of cache penetration? → Querying non-existent data, cache always misses.
4. **Quick Q&A 2**: Difference between cache penetration and cache breakdown? → Penetration is data doesn't exist; breakdown is hot key expires.
5. **Comprehensive**: Design a high-concurrency product detail page API. What cache strategies to consider? How to prevent penetration, breakdown, and avalanche?
```

### Learning Progress Tracking

Mark completion status with `- [x]` before each knowledge point in Layer 2:

```markdown
- [x] B+ Tree Index: How it works, range queries, leftmost prefix principle     ✅ 4/4 steps done
- [ ] Hash Index: Equality queries, no range support                              ⬜ 2/4 steps done
- [ ] Covering Index: All query fields in index, no table lookup                  ⬜ Not started
```

---

## 🗺️ Senior Engineer Skills Map (Top-Level Directory)

> Senior is not "how many frameworks you know" — it's "can you design systems, solve problems, and make technical decisions".
> The following 10 domains are the core competencies for Senior interviews and real-world work.

### Domain 1: System Design & Architecture

| Topic | Description | Priority |
|---|---|---|
| [Architecture Decisions](#architecture-decisions) | Monolith vs Microservices, Layered Architecture, MonolithFirst | ⭐⭐⭐ |
| [Microservices Concepts](#microservices-concepts) | Service decomposition, Service Discovery, API Gateway, Circuit Breaking | ⭐⭐⭐ |
| Distributed Systems Fundamentals | CAP Theorem, Consistency models, Partition tolerance | ⭐⭐⭐ |
| High Availability Design | Leader-follower replication, Load balancing, Failover, Degradation | ⭐⭐⭐ |
| Capacity Planning | QPS estimation, Storage estimation, Bandwidth estimation | ⭐⭐⭐ |
| Architecture Evolution | Migration strategy from monolith to microservices | ⭐⭐ |
| CQRS / Event Sourcing | Read-write separation, Event sourcing | ⭐ |

### Domain 2: Database & Storage

| Topic | Description | Priority |
|---|---|---|
| [Database & Caching](#database--caching) | PostgreSQL, Redis, Alibaba Standards | ⭐⭐⭐ |
| Index Optimisation | B+ Tree, Hash, GIN, GiST, Covering index, Composite index | ⭐⭐⭐ |
| Transaction Isolation Levels | ACID, Dirty read, Phantom read, MVCC, Optimistic/Pessimistic locking | ⭐⭐⭐ |
| Slow Query Optimisation | EXPLAIN analysis, Index failure scenarios, SQL tuning | ⭐⭐⭐ |
| [Distributed Transactions](#distributed-transactions) | AT/TCC/Saga modes, Eventual consistency | ⭐⭐ |
| [Search Engine](#search-engine) | ElasticSearch, Inverted index, Tokenisation | ⭐⭐ |
| NoSQL Selection | MongoDB, Redis, Cassandra — when to use which | ⭐⭐ |
| Database Sharding | Horizontal/Vertical split, Shard key selection, Cross-shard queries | ⭐ |
| Connection Pooling | Pool configuration, Connection leak debugging | ⭐⭐ |

### Domain 3: Backend Development

| Topic | Description | Priority |
|---|---|---|
| [Backend Technologies](#backend-technologies) | ASP.NET Core, EF Core, Swagger | ⭐⭐⭐ |
| RESTful API Design | Naming conventions, Versioning, HATEOAS, Pagination | ⭐⭐⭐ |
| [Authentication & Security](#authentication--security) | JWT, OAuth2, RBAC, Password hashing | ⭐⭐⭐ |
| [Design Patterns](#design-patterns) | Strategy, Factory, Observer, Singleton, Repository | ⭐⭐⭐ |
| Dependency Injection | IoC container, Lifetimes (Singleton/Scoped/Transient) | ⭐⭐⭐ |
| Async Programming | async/await, Task, Deadlock debugging, ConfigureAwait | ⭐⭐⭐ |
| Error Handling | Global exception handling, Error code standards, Retry policies | ⭐⭐⭐ |
| Rate Limiting & Circuit Breaking | Token bucket, Sliding window, Polly, Sentinel | ⭐⭐ |
| [Message Queues & Async Processing](#message-queues--async-processing) | RabbitMQ, Kafka, Dead letter queues, Message idempotency | ⭐⭐ |
| gRPC / GraphQL | When to use REST vs gRPC vs GraphQL | ⭐ |

### Domain 4: Frontend Development

| Topic | Description | Priority |
|---|---|---|
| [Frontend Technologies](#frontend-technologies) | Next.js, React, TypeScript | ⭐⭐⭐ |
| State Management | Context, Zustand, Redux — selection criteria | ⭐⭐⭐ |
| [Performance Optimisation](#performance-optimisation) | Lazy loading, Code splitting, Virtual lists, Debounce/Throttle | ⭐⭐⭐ |
| [CSS & Responsive Design](#css--responsive-design) | CSS Grid, Flexbox, rem, Media queries | ⭐⭐ |
| [PWA](#pwa) | Service Worker, Offline caching, Manifest | ⭐⭐ |
| SSR / SSG / ISR | Server-side rendering vs Static generation vs Incremental rendering | ⭐⭐ |
| Web Security | XSS, CSRF, CSP, CORS, SRI | ⭐⭐⭐ |
| Browser Rendering | Critical rendering path, Reflow/Repaint, Compositing layers | ⭐ |

### Domain 5: Testing & Quality

| Topic | Description | Priority |
|---|---|---|
| [Testing](#testing) | xUnit, Vitest, React Testing Library | ⭐⭐⭐ |
| Test Strategy | Test pyramid (unit > integration > E2E), Coverage | ⭐⭐⭐ |
| Mocking & Stubbing | When to mock, How to mock external dependencies | ⭐⭐⭐ |
| TDD / BDD | Test-Driven Development, Behaviour-Driven Development | ⭐⭐ |
| Load Testing | JMeter, k6, Locust | ⭐⭐ |
| Code Quality | Code Review process, Static analysis, Linting | ⭐⭐⭐ |
| Chaos Engineering | Chaos Monkey, Fault injection | ⭐ |

### Domain 6: DevOps & Operations

| Topic | Description | Priority |
|---|---|---|
| [DevOps & Deployment](#devops--deployment) | Docker, AWS, CI/CD | ⭐⭐⭐ |
| Container Orchestration | Docker Compose → Kubernetes evolution | ⭐⭐ |
| Infrastructure as Code | Terraform, CloudFormation | ⭐⭐ |
| Logging & Monitoring | ELK Stack, Grafana, Prometheus, CloudWatch | ⭐⭐⭐ |
| Distributed Tracing | OpenTelemetry, Jaeger, SkyWalking | ⭐⭐ |
| Alerting & On-call | Alert rules, P0/P1/P2 severity, On-call rotation | ⭐⭐ |
| Blue-Green / Canary Deployment | Zero-downtime deployment strategies | ⭐⭐ |
| Git Workflow | GitFlow, Trunk-Based, PR Review process | ⭐⭐⭐ |

### Domain 7: Performance Engineering

| Topic | Description | Priority |
|---|---|---|
| [Performance Optimisation](#performance-optimisation) | Lazy loading, Thread pool, Code splitting | ⭐⭐⭐ |
| Profiling Tools | dotnet-trace, BenchmarkDotNet, Lighthouse | ⭐⭐⭐ |
| Memory Management | GC mechanism, Memory leak debugging, IDisposable | ⭐⭐ |
| Concurrency | Locks, Semaphore, Channel, Concurrent collections | ⭐⭐ |
| CDN & Caching Strategy | HTTP cache headers, CDN configuration, Cache invalidation | ⭐⭐ |
| Database Performance | Connection pooling, Batch operations, N+1 query problem | ⭐⭐⭐ |

### Domain 8: Security

| Topic | Description | Priority |
|---|---|---|
| [Authentication & Security](#authentication--security) | JWT, bcrypt, HttpOnly Cookie | ⭐⭐⭐ |
| OWASP Top 10 | SQL Injection, XSS, CSRF, SSRF, Insecure deserialisation | ⭐⭐⭐ |
| Secret Management | Environment variables, Vault, Key rotation | ⭐⭐ |
| Network Security | HTTPS, TLS, Certificate management | ⭐⭐ |
| Secure Coding | Input validation, Output encoding, Least privilege | ⭐⭐⭐ |
| Compliance | GDPR, PCI DSS (payments), SOC 2 | ⭐ |

### Domain 9: Soft Skills & Engineering Culture

| Topic | Description | Priority |
|---|---|---|
| Code Review | How to review code, How to write good PRs | ⭐⭐⭐ |
| Technical Documentation | ADR (Architecture Decision Records), RFC, Design docs | ⭐⭐⭐ |
| Technical Decision-Making | How to evaluate and choose technical solutions | ⭐⭐⭐ |
| Incident Post-mortem | Root cause analysis, Improvement actions | ⭐⭐ |
| Knowledge Sharing | Tech talks, Technical blog writing | ⭐⭐ |
| Cross-team Collaboration | API contracts, Technical design reviews, Requirement alignment | ⭐⭐ |
| Mentoring | How to mentor Juniors, Knowledge transfer | ⭐ |

### Domain 10: Payment & E-commerce Domain

| Topic | Description | Priority |
|---|---|---|
| [Payment Integration](#payment-integration) | Stripe, Webhooks, Tokenisation | ⭐⭐⭐ |
| Idempotency | Payment callback idempotency, Double-charge prevention | ⭐⭐⭐ |
| Reconciliation | Payment reconciliation, Exception handling, Refund flow | ⭐⭐ |
| E-commerce Domain Model | Product, SKU, Inventory, Order state machine | ⭐⭐ |

---

## 📖 Layer 2: Detailed Knowledge Points

> Below is the detailed knowledge point checklist for each domain. Learn progressively, check off when done.

---

### Domain 1: System Design & Architecture

#### 1.1 Architecture Decisions
- [ ] MonolithFirst pattern: Strategy and timing for monolith-to-microservice transition
- [ ] Modular Monolith: Module boundaries, independent schemas, event-driven decoupling
- [ ] Strangler Fig pattern: Incremental migration from legacy to new architecture
- [ ] ADR (Architecture Decision Records): How to write ADR, template format, trade-off analysis

#### 1.2 Microservices Architecture
- [ ] Service decomposition principles: By domain (DDD Bounded Context) / by capability / by data ownership
- [ ] Service Discovery: How Consul, Eureka, Kubernetes Service work
- [ ] API Gateway: Routing, rate limiting, authentication, protocol translation (YARP, Ocelot, Kong)
- [ ] Circuit Breaker pattern: Open → Half-Open → Closed state transitions, Polly implementation
- [ ] Inter-service communication: Sync (HTTP/gRPC) vs async (message queue) selection
- [ ] Saga pattern: Orchestration vs Choreography
- [ ] Service Mesh: Istio, Linkerd concepts and use cases

#### 1.3 Distributed Systems Fundamentals
- [ ] CAP theorem: Consistency, Availability, Partition tolerance trade-offs
- [ ] PACELC model: Extension of CAP considering latency
- [ ] Consistency models: Strong, eventual, causal consistency
- [ ] Consensus algorithms: Raft, Paxos basics (understand Leader election, no need to implement)
- [ ] Vector Clocks: Distributed event ordering
- [ ] Distributed ID generation: Snowflake, UUID v7, database auto-increment vs globally unique

#### 1.4 High Availability Design
- [ ] Leader-Follower replication: Async vs sync replication, replication lag
- [ ] Multi-leader replication: Conflict resolution strategies
- [ ] Load balancing algorithms: Round-robin, weighted round-robin, least connections, consistent hashing
- [ ] Health checks: Active probing vs passive probing, graceful shutdown
- [ ] Failover: Manual vs automatic, split-brain problem
- [ ] Graceful degradation: Core features priority, non-core circuit breaking, return cached/default values
- [ ] Rate limiting strategies: Token bucket, leaky bucket, sliding window, fixed window

#### 1.5 Capacity Planning
- [ ] Little's Law: L = λW (avg requests in system = arrival rate × avg processing time)
- [ ] QPS / TPS estimation: How to derive system load from user count
- [ ] Storage estimation: Data growth prediction, hot/cold data separation
- [ ] Bandwidth estimation: Images, videos, API response bandwidth requirements
- [ ] Latency percentiles: Meaning and usage of p50, p95, p99
- [ ] Load testing methodology: Benchmark, load test, stress test, soak test

#### 1.6 CQRS & Event Sourcing
- [ ] CQRS: Command (write) and Query (read) separation, independent models
- [ ] Event Sourcing: Store events instead of state, event replay
- [ ] Projection: Building read models from event streams
- [ ] Snapshot: Avoiding slow event replay

---

### Domain 2: Database & Storage

#### 2.1 Index Optimisation
- [ ] B+ Tree index: How it works, range queries, leftmost prefix principle
- [ ] Hash index: Equality queries, no range support
- [ ] Covering Index: All query fields in index, no table lookup needed
- [ ] Composite Index: Column order, index failure scenarios
- [ ] Partial Index: Index only rows matching conditions
- [ ] Expression Index: Index on function results
- [ ] Index failures: Function operations, implicit type conversion, LIKE '%xx', OR conditions

#### 2.2 Transaction Isolation Levels
- [ ] ACID: Atomicity, Consistency, Isolation, Durability
- [ ] Isolation levels: Read Uncommitted → Read Committed → Repeatable Read → Serializable
- [ ] Dirty read, non-repeatable read, phantom read: Differences and causes
- [ ] MVCC: PostgreSQL implementation, snapshot isolation
- [ ] Optimistic vs Pessimistic locking: Version number, SELECT FOR UPDATE, use cases
- [ ] Deadlock: Conditions, detection methods, prevention strategies

#### 2.3 Slow Query Optimisation
- [ ] EXPLAIN / EXPLAIN ANALYZE: How to read execution plans
- [ ] Seq Scan vs Index Scan vs Index Only Scan
- [ ] N+1 query problem: How to detect, how to fix (JOIN / eager loading)
- [ ] Subquery vs JOIN vs CTE performance differences
- [ ] Batch operations: Batch INSERT, batch UPDATE, avoid row-by-row operations
- [ ] Query caching: Redis cache query results, cache invalidation strategies

#### 2.4 Database Design
- [ ] Normalisation (1NF → 2NF → 3NF) vs Denormalisation
- [ ] Data type selection: BIGINT vs INT, DECIMAL vs FLOAT, VARCHAR vs TEXT
- [ ] Naming conventions: snake_case, table name plural/singular, index naming
- [ ] Soft Delete vs Hard Delete: is_deleted field, data recovery
- [ ] Audit fields: created_at, updated_at, created_by
- [ ] JSON/JSONB fields: When to use, how to index, performance impact

#### 2.5 Redis Deep Dive
- [ ] Data types: String, Hash, List, Set, Sorted Set, Stream, HyperLogLog
- [ ] Eviction policies: LRU, LFU, TTL, noeviction
- [ ] Persistence: RDB snapshot vs AOF log, hybrid persistence
- [ ] Redis Cluster: Sharding principles, consistent hashing, data migration
- [ ] Redis Sentinel: High availability, automatic failover
- [ ] Cache penetration, breakdown, avalanche: Differences and solutions
- [ ] Distributed lock: Redlock algorithm, lock renewal, reentrant lock

#### 2.6 Connection Pooling
- [ ] Pool parameters: Min connections, max connections, idle timeout
- [ ] Connection leak debugging: Unclosed connections, using statements
- [ ] Pool monitoring: Active connections, wait queue length
- [ ] PgBouncer: PostgreSQL connection pool middleware

---

### Domain 3: Backend Development

#### 3.1 RESTful API Design
- [ ] Naming conventions: Plural nouns, lowercase, hyphens (/api/order-items)
- [ ] HTTP method semantics: GET (idempotent), POST (non-idempotent), PUT (full replace), PATCH (partial update), DELETE
- [ ] Status code conventions: 2xx success, 3xx redirect, 4xx client error, 5xx server error
- [ ] Versioning: URL versioning (/v1/) vs header versioning (Accept-Version)
- [ ] Pagination: Offset vs Cursor pagination, total count, Link header
- [ ] Filtering & sorting: Query parameter conventions (?status=active&sort=-created_at)
- [ ] HATEOAS: Hypermedia-driven REST (understand concept only)
- [ ] API rate limiting: RateLimit response headers, 429 Too Many Requests

#### 3.2 Async Programming
- [ ] async/await: State machine principle, SynchronizationContext
- [ ] Task vs ValueTask: When to use which
- [ ] Deadlock debugging: .Result, .Wait(), ConfigureAwait(false)
- [ ] CancellationToken: Timeout cancellation, graceful shutdown
- [ ] Parallel.ForEach / Task.WhenAll: Parallel execution
- [ ] Channel<T>: Producer-consumer pattern
- [ ] IAsyncEnumerable: Async streaming data

#### 3.3 Error Handling
- [ ] Global exception handling middleware: Exception type → status code mapping
- [ ] Custom exception classes: BusinessException, NotFoundException
- [ ] Error response format: RFC 7807 Problem Details
- [ ] Retry strategy: Exponential backoff, jitter
- [ ] Circuit breaker strategy: Break after N consecutive failures, half-open probe
- [ ] Degradation strategy: Return cached data, default values, friendly messages

#### 3.4 Dependency Injection
- [ ] IoC container principle: Inversion of Control, DI (constructor injection)
- [ ] Lifetimes: Singleton (global) → Scoped (per request) → Transient (per resolution)
- [ ] Lifetime pitfalls: Singleton cannot inject Scoped services
- [ ] Factory pattern injection: IServiceProvider, ActivatorUtilities
- [ ] Decorator pattern injection: Dynamic implementation replacement

#### 3.5 Message Queues Deep Dive
- [ ] RabbitMQ: Exchange (Direct/Topic/Fanout/Headers), Queue, Binding
- [ ] Message acknowledgement: Ack/Nack, manual vs auto acknowledgement
- [ ] Dead Letter Queue (DLQ): Handling failed message consumption
- [ ] Message idempotency: Consumer-side deduplication, unique message ID
- [ ] Message ordering: Partition-ordered vs globally ordered
- [ ] Kafka: Topic, Partition, Consumer Group, Offset management
- [ ] Eventual consistency: How to ensure cross-service data consistency

---

### Domain 4: Frontend Development

#### 4.1 Next.js Deep Dive
- [ ] App Router: Layout nesting, loading UI, error boundaries
- [ ] Server Components vs Client Components: When to use which
- [ ] Server Actions: Form submission, data mutation
- [ ] Middleware: Auth checks, redirects, A/B testing
- [ ] Data fetching: fetch caching, revalidate, no-store
- [ ] Image component: Auto optimisation, lazy loading, responsive images
- [ ] Internationalisation: i18n routing, dynamic language pack imports

#### 4.2 React Deep Dive
- [ ] Virtual DOM & Reconciliation: Diff algorithm, role of Key
- [ ] Hooks internals: useState, useEffect, useRef, useMemo, useCallback
- [ ] Closure traps: Stale closure, how to avoid
- [ ] Concurrent features: Suspense, useTransition, useDeferredValue
- [ ] Performance: React.memo, useMemo, useCallback correct usage
- [ ] Error boundaries: ErrorBoundary, fallback UI

#### 4.3 TypeScript
- [ ] Basic types: string, number, boolean, any, unknown, never
- [ ] Interface vs Type alias: When to use interface, when to use type
- [ ] Generics: Generic functions, generic constraints, conditional types
- [ ] Utility types: Partial, Required, Pick, Omit, Record, Exclude
- [ ] Type guards: typeof, instanceof, custom type guards
- [ ] Module declarations: .d.ts files, declaration merging

#### 4.4 State Management
- [ ] Context API: Provider, useContext, performance issues (unnecessary re-renders)
- [ ] Zustand: create, subscribe, devtools, persist
- [ ] Redux Toolkit: createSlice, RTK Query
- [ ] React Query / TanStack Query: Server state management, caching, optimistic updates
- [ ] URL state: Search params, route state

#### 4.5 Performance Optimisation
- [ ] Lazy loading: dynamic import, React.lazy, Suspense
- [ ] Code splitting: Route-level, component-level
- [ ] Virtual lists: react-window, react-virtuoso (large list rendering)
- [ ] Debounce & Throttle: Search input, scroll events
- [ ] Web Workers: Offload CPU-intensive tasks to background threads
- [ ] Image optimisation: WebP/AVIF formats, responsive images, CDN

#### 4.6 Web Security (Frontend)
- [ ] XSS: Reflected, stored, DOM-based, defence (output encoding, CSP)
- [ ] CSRF: Attack mechanism, defence (SameSite Cookie, CSRF Token)
- [ ] CSP (Content Security Policy): Restrict resource loading sources
- [ ] CORS: Same-origin policy, preflight requests, Access-Control headers
- [ ] SRI (Subresource Integrity): Verify external resource integrity

#### 4.7 Browser Rendering
- [ ] Critical rendering path: HTML → DOM → CSSOM → Render Tree → Layout → Paint → Composite
- [ ] Reflow vs Repaint: What triggers them, how to reduce
- [ ] Compositing layers: will-change, transform, opacity
- [ ] Long Tasks: How to detect, how to break up

---

### Domain 5: Testing & Quality

#### 5.1 Test Strategy
- [ ] Test pyramid: Lots of unit tests → moderate integration → few E2E
- [ ] Test coverage: Line coverage, branch coverage, target 80%+
- [ ] FIRST principles: Fast, Isolated, Repeatable, Self-validating, Timely
- [ ] AAA pattern: Arrange → Act → Assert
- [ ] Test naming: Method_Scenario_ExpectedResult

#### 5.2 Mocking & Stubbing
- [ ] Test Double types: Dummy, Stub, Spy, Mock, Fake
- [ ] When to mock: External dependencies (database, HTTP, file system)
- [ ] When not to mock: Pure logic, value objects, simple calculations
- [ ] .NET: Moq, NSubstitute
- [ ] JavaScript: Vitest mock, MSW (Mock Service Worker)

#### 5.3 Load Testing
- [ ] JMeter: GUI recording, parameterisation, distributed load testing
- [ ] k6: Scripting, scenario configuration, metric interpretation
- [ ] Locust: Python-based, Web UI
- [ ] Key metrics: Throughput (RPS), latency (p50/p95/p99), error rate

#### 5.4 Code Quality
- [ ] Code Review Checklist: Correctness, security, performance, readability, testing
- [ ] Static analysis: SonarQube, ESLint, Stylelint
- [ ] Code formatting: Prettier, EditorConfig
- [ ] Conventional Commits: feat/fix/chore/test/docs prefixes
- [ ] PR standards: Small PRs, clear descriptions, linked issues

---

### Domain 6: DevOps & Operations

#### 6.1 Docker Deep Dive
- [ ] Dockerfile best practices: Multi-stage builds, layer caching, minimal images
- [ ] Docker Compose: Service orchestration, networking, volumes, environment variables
- [ ] Image security: Vulnerability scanning, minimal base images (Alpine, distroless)
- [ ] Container logging: stdout/stderr, log drivers

#### 6.2 Kubernetes Basics
- [ ] Pod: Smallest scheduling unit, multi-container pods
- [ ] Service: ClusterIP, NodePort, LoadBalancer
- [ ] Deployment: Rolling update, rollback, HPA auto-scaling
- [ ] Ingress: Routing rules, TLS termination
- [ ] ConfigMap / Secret: Configuration and secret management

#### 6.3 Logging & Monitoring
- [ ] Structured logging: JSON format, log levels, correlation IDs
- [ ] ELK Stack: Elasticsearch + Logstash + Kibana
- [ ] Prometheus + Grafana: Metric collection, dashboards, alerting
- [ ] RED method: Rate, Error, Duration
- [ ] USE method: Utilisation, Saturation, Errors

#### 6.4 Distributed Tracing
- [ ] OpenTelemetry: Traces, Spans, Context Propagation
- [ ] Jaeger / Zipkin: Distributed trace visualisation
- [ ] Correlation ID: Cross-service request tracking

#### 6.5 Deployment Strategies
- [ ] Blue-Green deployment: Two environments, instant switch, quick rollback
- [ ] Canary deployment: Small traffic percentage validation, gradual rollout
- [ ] Rolling update: Gradual replacement of old instances
- [ ] Feature Flag: Feature toggles, gradual release

#### 6.6 Git Workflow
- [ ] GitFlow: main/develop/feature/release/hotfix branches
- [ ] Trunk-Based: Mainline development, short-lived branches
- [ ] PR Review: How to write PR descriptions, how to do Code Review
- [ ] Semantic Versioning: SemVer (major.minor.patch)

---

### Domain 7: Performance Engineering

#### 7.1 Profiling Tools
- [ ] dotnet-trace: CPU sampling, event tracing
- [ ] BenchmarkDotNet: Micro-benchmarks, memory diagnostics
- [ ] Lighthouse: Web performance scoring (LCP, FID, CLS)
- [ ] Chrome DevTools: Performance panel, Memory panel

#### 7.2 Memory Management
- [ ] GC mechanism: Generation 0/1/2, Large Object Heap (LOH)
- [ ] IDisposable pattern: Dispose, using statement, Finalizer
- [ ] Memory leak debugging: dotnet-dump, dotnet-gcroot
- [ ] WeakReference: Caching scenarios, avoiding memory leaks
- [ ] ArrayPool / MemoryPool: Reducing array allocations

#### 7.3 Concurrency
- [ ] lock / Monitor: Mutual exclusion, deadlock prevention
- [ ] SemaphoreSlim: Semaphore, limiting concurrency
- [ ] ReaderWriterLockSlim: Read-write lock
- [ ] ConcurrentDictionary / ConcurrentQueue: Thread-safe collections
- [ ] Channel<T>: Producer-consumer, bounded/unbounded
- [ ] Interlocked: Atomic operations

#### 7.4 Database Performance
- [ ] Connection pool configuration: Max connections, connection timeout
- [ ] Batch operations: EF Core BulkExtensions, COPY command
- [ ] N+1 problem: Include/ThenInclude, projection queries
- [ ] Read-write splitting: Write to primary, read from replica, consistency considerations
- [ ] Query result caching: Redis cache, cache invalidation strategies

---

### Domain 8: Security

#### 8.1 OWASP Top 10
- [ ] SQL Injection: Parameterised queries, ORM protection, input validation
- [ ] XSS: Output encoding, CSP headers, HttpOnly Cookie
- [ ] CSRF: SameSite Cookie, CSRF Token, double submit
- [ ] SSRF: Whitelist validation, block internal network access
- [ ] Insecure deserialisation: Avoid deserialising untrusted data
- [ ] Sensitive data exposure: Encrypted storage, transport encryption, log redaction
- [ ] Broken access control: Least privilege, RBAC, resource-level permissions

#### 8.2 Secret Management
- [ ] Environment variables: .env for dev, Secret Manager for production
- [ ] Azure Key Vault / AWS Secrets Manager: Centralised secret management
- [ ] Key rotation: Periodic replacement, automatic rotation
- [ ] Secret access control: Least privilege, audit logs

#### 8.3 Secure Coding
- [ ] Input validation: Whitelist validation, length limits, type checking
- [ ] Output encoding: HTML encoding, URL encoding, JavaScript encoding
- [ ] SQL injection prevention: Parameterised queries, stored procedures, ORM
- [ ] Log security: Don't log passwords, tokens, card numbers
- [ ] Dependency security: npm audit, dotnet list package --vulnerable

---

### Domain 9: Soft Skills & Engineering Culture

#### 9.1 Code Review
- [ ] Review Checklist: Functional correctness, edge cases, security vulnerabilities, performance, code style
- [ ] How to give feedback: Specific, actionable, focus on code not person
- [ ] PR size: One PR = one concern, 200-400 lines
- [ ] Self-Review: Review your own code before requesting review

#### 9.2 Technical Documentation
- [ ] ADR (Architecture Decision Records): Title, context, decision, consequences
- [ ] RFC (Request for Comments): Proposal process, discussion, approval
- [ ] API documentation: OpenAPI spec, Swagger UI
- [ ] Architecture diagrams: C4 model (Context → Container → Component → Code)
- [ ] README: Project overview, quick start, API reference

#### 9.3 Technical Decision-Making
- [ ] Evaluation dimensions: Performance, community activity, learning curve, maintenance cost, licence
- [ ] POC (Proof of Concept): Validate key technical points with minimal code
- [ ] Spike: Time-boxed research, output conclusions
- [ ] Decision matrix: List candidates, score, weight

#### 9.4 Incident Post-mortem
- [ ] Blameless Post-mortem: Focus on system, not individuals
- [ ] 5 Whys analysis: Ask "why" repeatedly to find root cause
- [ ] Timeline documentation: Detection → Response → Diagnosis → Fix → Recovery
- [ ] Improvement actions: Short-term fix, long-term prevention, owner and deadline

---

### Domain 10: Payment & E-commerce Domain

#### 10.1 Stripe Payment
- [ ] Payment Intents: Create → Confirm → Capture flow
- [ ] Checkout Session: Hosted payment page
- [ ] Webhook signature verification: Prevent forgery, replay attacks
- [ ] 3D Secure (SCA): Strong Customer Authentication
- [ ] Refunds: Full refund, partial refund, refund status tracking
- [ ] Disputes: Handling process, evidence submission

#### 10.2 Idempotency
- [ ] Idempotency concept: Same request executed multiple times, same result
- [ ] Idempotency Key: Client-generated, server-stored
- [ ] Payment idempotency: Prevent double charges
- [ ] Webhook idempotency: Stripe may send same event multiple times

#### 10.3 E-commerce Domain Model
- [ ] Product & SKU: Product → SKU (variant) → Inventory
- [ ] Inventory management: Reserve on order, deduct on payment, release on timeout
- [ ] Order state machine: pending → paid → processing → shipped → completed → cancelled
- [ ] Pricing strategy: Original price, sale price, coupons, bulk discounts

---

## 📚 Project-Specific Notes (By Technology)

> Below are the detailed technology notes for the Novacart project.

- [Architecture Decisions](#architecture-decisions)
- [Backend Technologies](#backend-technologies)
- [Frontend Technologies](#frontend-technologies)
- [Database & Caching](#database--caching)
- [Authentication & Security](#authentication--security)
- [Message Queues & Async Processing](#message-queues--async-processing)
- [Search Engine](#search-engine)
- [Design Patterns](#design-patterns)
- [Testing](#testing)
- [DevOps & Deployment](#devops--deployment)
- [Performance Optimisation](#performance-optimisation)
- [CSS & Responsive Design](#css--responsive-design)
- [PWA](#pwa)
- [Payment Integration](#payment-integration)
- [Microservices Concepts](#microservices-concepts)
- [Distributed Transactions](#distributed-transactions)
- [Interview Cheat Sheet](#interview-cheat-sheet)
- [Official Documentation Links](#official-documentation-links)
- [Project Practice Log](#project-practice-log)

---

## Project Practice Log

> Record of technologies used, pitfalls encountered, and lessons learned during project development.

### 2026-06-04: Project Skeleton Setup

#### 1. Docker Compose Multi-Service Orchestration

**What was done:** Used a single `docker-compose.yml` to orchestrate 4 services: PostgreSQL, Redis, ASP.NET Core backend, Next.js frontend.

**Key configuration:**

```yaml
services:
  postgres:
    image: postgres:16-alpine
    healthcheck:                                    # ① Health check
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5

  backend:
    depends_on:
      postgres:
        condition: service_healthy                  # ② Wait for dependency to be healthy
      redis:
        condition: service_healthy
    environment:                                    # ③ Env vars override config
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;...
      - ConnectionStrings__Redis=redis:6379

  frontend:
    depends_on:
      - backend                                     # ④ Simple dependency (no health wait)
```

**Lessons learned:**
- `healthcheck` ensures the database is truly ready before starting the backend, avoiding connection failures
- `condition: service_healthy` is more reliable than simple `depends_on`
- Inter-container communication uses **service names** (`postgres`, `redis`, `backend`), not `localhost`
- Environment variables use `__` double underscore to map to .NET nested config (`ConnectionStrings:DefaultConnection`)

#### 2. Dockerfile Multi-Stage Builds

**Backend (ASP.NET Core):**

```dockerfile
# Build stage — use SDK image to compile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj ./           # Copy csproj first
RUN dotnet restore          # Restore dependencies separately (layer cache)
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage — use lightweight ASP.NET image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Novacart.Api.dll"]
```

**Frontend (Next.js):**

```dockerfile
# Build stage
FROM node:20-alpine AS build
COPY package.json ./
RUN npm install             # Install deps first (layer cache)
COPY . .
RUN npm run build

# Runtime stage
FROM node:20-alpine
COPY --from=build /app/.next ./.next
COPY --from=build /app/node_modules ./node_modules
CMD ["npm", "start"]
```

**Lessons learned:**
- Multi-stage builds make final images much smaller (backend ~210MB vs SDK ~800MB)
- **Copy dependency files first, then COPY all code** — when deps haven't changed, `npm install` / `dotnet restore` uses cache
- `.dockerignore` excludes `node_modules`, `bin`, `obj` to avoid sending unnecessary context

#### 3. Next.js API Proxy

**What was done:** Frontend `/api/*` requests automatically forwarded to backend `backend:5000`.

```javascript
// next.config.mjs
const nextConfig = {
  async rewrites() {
    return [
      {
        source: '/api/:path*',                       // Frontend requests /api/xxx
        destination: 'http://backend:5000/api/:path*', // Forward to backend
      },
    ];
  },
};
```

**Lessons learned:**
- `rewrites` is a server-side proxy — browser doesn't see the backend address, avoiding CORS issues
- Dev and Docker environments have different backend addresses: Docker uses `backend:5000`, local dev uses `localhost:5000`
- Production should use Nginx reverse proxy instead of Next.js rewrites

#### 4. ASP.NET Core Dependency Injection Configuration

**What was done:** Registered DbContext, Redis, CORS, Swagger in `Program.cs`.

```csharp
// PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis — registered as Singleton (connection pool reuse)
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(config));

// CORS — allow frontend cross-origin access
builder.Services.AddCors(options =>
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()));
```

**Lessons learned:**
- `AddDbContext` defaults to Scoped lifetime (one instance per request)
- `IConnectionMultiplexer` registered as Singleton (Redis connection pool, global reuse)
- CORS must be applied with `app.UseCors()` — order matters
- `WithOrigins("http://localhost:3000")` only allows the frontend domain; production should use the real domain

#### 5. Pitfalls Encountered

| Pitfall | Cause | Solution |
|---|---|---|
| Next.js error `next.config.ts is not supported` | Next.js 14.2.0 doesn't support `.ts` config files | Changed to `next.config.mjs` |
| Backend fails to connect to PostgreSQL | Container not ready when backend starts | Added `healthcheck` + `condition: service_healthy` |
| Frontend requests to backend return 404 | Docker uses `localhost:5000` which can't reach backend container | Changed to service name `backend:5000` |

#### 6. Command Quick Reference

```bash
# Start all services
docker compose up --build -d

# Check service status
docker compose ps

# View logs (specific service)
docker compose logs -f backend

# Stop all services
docker compose down

# Stop and delete data volumes
docker compose down -v

# Rebuild specific service
docker compose up --build -d backend
```

---

### 2026-07-15: P2/P3 Features — JWT Refresh, Async Email, S3, and the Bugs They Surfaced

> Record of the technical stack used for the P14 "preferred" deliverables, the real-world pitfalls
> hit during end-to-end verification, and the design decisions behind each. These are the kinds of
> issues that only appear when you actually run the full stack — not when unit tests pass in isolation.

#### 1. JWT Refresh Tokens — Rotation & Reuse Detection

**Stack:** PostgreSQL `refresh_tokens` table · opaque tokens hashed with SHA-256 · dual HttpOnly cookies · `RandomNumberGenerator` for entropy.

**What was done:** Implemented short-lived access tokens (15 min) + long-lived refresh tokens (7 days) with rotation. On refresh, the old token is revoked and a new pair issued; if a *revoked* token is presented again, the entire token family for that user is revoked (reuse detection — a stolen-token signal).

**Key design: dual cookies with different scopes**
```
novacart_jwt      → Path=/api        (15 min, sent on every API request)
novacart_refresh  → Path=/api/auth   (7 day, sent ONLY to auth endpoints)
```
Narrowing the refresh cookie's `Path` to `/api/auth` means it is never sent on product/cart/order requests — minimising its exposure surface. The access token cookie stays at `/api` so `[Authorize]` endpoints read it via `OnMessageReceived`.

**Lessons learned:**
- **Hash refresh tokens, never store them raw.** Unlike passwords (bcrypt), refresh tokens are high-entropy random values, so a plain SHA-256 hash is sufficient and faster — bcrypt's slow KDF is pointless when the input is already 256 bits of randomness.
- **Reuse detection is the whole point of rotation.** If you rotate but don't detect reuse, a stolen token still works after one rotation. Detecting a *revoked* token being reused lets you assume compromise and revoke everything.
- **ClockSkew matters.** The JWT bearer validation uses `ClockSkew = 30s`. For refresh flows you want access tokens to expire *promptly* — a large clock skew would undermine the short access-token lifetime.
- **Frontend refresh must be coalesced.** When the access token expires, many in-flight requests 401 simultaneously. Without a shared `refreshPromise`, you'd trigger N parallel refresh calls (each rotating the token, invalidating the others). A module-level singleton promise collapses them to one.

**The bug the frontend caught:**
```
// WRONG — N concurrent 401s trigger N refreshes, each invalidating the prior
if (res.status === 401) { await refresh(); retry(); }

// RIGHT — one in-flight refresh; all waiters share its result
let refreshPromise = null;
async function tryRefresh() {
  if (!refreshPromise) refreshPromise = doRefresh().finally(() => refreshPromise = null);
  return refreshPromise;
}
```

#### 2. Async Email Queue — `Channel<T>` + `BackgroundService`

**Stack:** `System.Threading.Channels.Channel<EmailMessage>` (bounded, 256, `Wait` back-pressure) · `BackgroundService` worker · scoped `EmailService` (MailKit SMTP).

**What was done:** Decoupled email sending from request/webhook handling. `PaymentService` and `AdminOrderService` enqueue a snapshot `EmailMessage` and return immediately; the `EmailBackgroundWorker` drains the channel on a background thread.

**Key design: the worker runs in its own DI scope**
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    await foreach (var message in _queue.ReadAllAsync(stoppingToken))
    {
        using var scope = _scopeFactory.CreateScope();           // ← fresh scope per message
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();
        // ... send ...
    }
}
```
The worker is a **Singleton** (hosted service), but `EmailService` is **Scoped** (needs `AppDbContext`/config per request). Resolving a scoped service from a singleton throws a captive-dependency error — so the worker must create its own scope via `IServiceScopeFactory` for each message.

**Lessons learned:**
- **Never pass EF entities across scopes.** The `Order` loaded in the webhook handler's scope is disposed by the time the worker processes the message. The `EmailMessage` record captures *only the primitive fields* (email, order number, total, status) — a snapshot, not a reference.
- **Bounded channels apply back-pressure.** `BoundedChannelFullMode.Wait` makes `EnqueueAsync` block when full, naturally slowing producers under load instead of OOM-ing with an unbounded queue.
- **A failed send must not kill the worker.** Each iteration is wrapped in try/catch; the worker logs and moves on. Otherwise one bad SMTP response poisons the entire queue.

**The pre-existing bug surfaced by this work:**
`docker-compose.prod.yml` set `Smtp__FromAddress`, but `EmailService` read `Smtp:FromEmail` — the sender address was silently ignored in production. The keys didn't match. Fixed by reading both: `config["Smtp:FromEmail"] ?? config["Smtp:FromAddress"]`.

#### 3. S3 Object Storage with LocalStack — No AWS Account Needed

**Stack:** `AWSSDK.S3` · `IS3StorageService` (config-driven: LocalStack or real AWS) · presigned PUT/GET URLs · `localstack/localstack:3.0` in Docker.

**What was done:** Admin uploads product images directly to S3 (browser → presigned PUT URL → store returned URL). Backend never proxies file bodies. LocalStack emulates S3 locally so no AWS account/credentials are needed for dev.

**Key design: config-driven provider switching (zero code change)**
```csharp
// ServiceUrl set → LocalStack (or any S3-compatible store)
if (!string.IsNullOrEmpty(serviceUrl))
    _client = new AmazonS3Client(accessKey, secretKey, new AmazonS3Config {
        ServiceURL = serviceUrl, ForcePathStyle = true, UseHttp = ...
    });
else
    _client = new AmazonS3Client(RegionEndpoint.GetBySystemName(region));  // real AWS
```
Production = unset `Aws:S3:ServiceUrl`, rely on the default credential chain (IAM role / env vars). Same code, different config.

**Two real bugs caught only by end-to-end testing:**

| Pitfall | Cause | Solution |
|---|---|---|
| **`NoSuchBucket` on first upload** | LocalStack 3.x does **not** auto-create buckets from the `BUCKETS` env var (despite older docs) | Added `localstack/init-s3-bucket.sh` mounted to `/etc/localstack/init/ready.d/` — runs `awslocal s3 mb` when LocalStack becomes ready |
| **Presigned PUT URL used `https://`** but LocalStack serves plain HTTP → curl exit 60 (SSL error) | The AWS SDK emits `https` in presigned URLs by default, ignoring `UseHttp` for the URL string | Detect `ServiceUrl` starts with `http://` and post-process the generated URL: `url.Replace("https://", "http://")` |

**Lesson:** Presigned URLs are computed *locally* by the SDK (no network call), so they can be unit-tested without a running S3/LocalStack — but the **scheme/host correctness** only shows up in a real round-trip. Unit tests passing did not catch the https issue; only `curl PUT` against the URL did. This is why end-to-end verification of I/O-heavy features matters even when unit tests are green.

#### 4. Cross-Cutting: What "Tests Pass" Does Not Prove

The single biggest lesson from this session: **130/130 unit tests passing did not catch either S3 bug.** Unit tests verified the presign *logic* (key generation, config parsing) but not the *wire format* (https vs http, bucket existence). The LocalStack bucket-creation and URL-scheme issues only surfaced when running `curl PUT` against the real presigned URL in a live stack.

**Takeaway:** For features involving external systems (S3, SMTP, webhooks, payment gateways), a green unit-test suite is necessary but not sufficient. Always do at least one end-to-end round-trip (presign → PUT → GET) against a real (or LocalStack-emulated) service before declaring "done."

### 2026-07-16: PE-1 through PE-10 — Microservices → i18n

> Record of the **Planned Enhancements** rollout (PE-1 ~ PE-10): tech stacks, mistakes, and fixes. Canonical task lists: [TODO.md](TODO.md), [HANDOFF.md §11](HANDOFF.md#11-planned-enhancements-scaling-tail--not-scheduled). Design docs: [MICROSERVICES-PE1.md](docs/MICROSERVICES-PE1.md), [PE3-ELASTICSEARCH.md](docs/PE3-ELASTICSEARCH.md), [PE4-PRODUCTION-HARDENING.md](docs/PE4-PRODUCTION-HARDENING.md), [PE6-REDIS-CART.md](docs/PE6-REDIS-CART.md), [PE7-SQL-SHARDING.md](docs/PE7-SQL-SHARDING.md), [PE8-THREAD-POOL.md](docs/PE8-THREAD-POOL.md), [PE10-I18N.md](docs/PE10-I18N.md).

#### Tech stack overview (PE-1 ~ PE-7)

| PE | Scope | Stack |
|---|---|---|
| **PE-1** | Microservices (default `docker compose up`) | **.NET Aspire** AppHost · **YARP** gateway · **Polly** + **Refit** · **MassTransit** + **RabbitMQ** · **Transactional Outbox** · **MassTransit Saga** · 4 logical DBs · **OpenTelemetry** + **Jaeger** · K8s skeleton |
| **PE-2** | Async messaging | Folded into PE-1 — MassTransit transport |
| **PE-3** | Full-text search | **Elasticsearch 8.15** · Product API; Postgres `ILIKE` fallback |
| **PE-4** | Inventory hardening | **Redis 7** Redlock · **stock holds** (TTL) · **atomic SQL** decrement · **YARP rate limit** · Redis HA docs · **OTel** `Novacart.Stock` metrics |
| **PE-5** | Async checkout + ops | PE-1 Saga + **`CheckoutSagaAdminService`** · admin saga list · DLQ retry UI |
| **PE-6** | Cart cache | **Redis** JSON snapshot · Postgres source of truth · write-through · `CartRedis.Enabled=false` default |
| **PE-7** | Order SQL sharding | **UserId FNV-1a hash** · `novacart_commerce_0/1` · `order_shard_routes` routing table · `IShardedOrderDb` · `OrderSharding.Enabled=false` default |
| **PE-8** | Thread pool tuning | **`ThreadPool.SetMinThreads`** · optional **Stripe webhook queue** · OTel `Novacart.Runtime` · `ThreadPool.Enabled=false` default |
| **PE-10** | Internationalisation | **`next-intl`** · `/en/` + `/zh/` · admin form namespaces · **`hreflang`** via middleware pathname header |
| **PE-9** | AI chatbot | **`IChatCompletionClient`** · OpenAI / Claude / Ollama · FAQ fallback · opt-in widget |

#### 0. Process & verification mistakes (cross-cutting, PE-1 ~ PE-7)

| Mistake | Tech / context | Fix / lesson |
|---|---|---|
| **Committed on user's behalf** without explicit request | Git workflow | Only commit when the user asks. Use `git reset HEAD~1` to undo while keeping changes. |
| **Skipped Docker verification** citing "no local .NET 8 runtime" | HANDOFF §2 | The project standard is `docker compose down && docker compose up --build -d` + `db-migrate` + smoke tests — not host `dotnet run`. |
| **Misread exit code 139** as SIGSEGV / native crash | .NET in Linux containers | Read container logs first. In this project, 139 was **unhandled startup exceptions** (EF model + DI), not a segfault. |
| **Stale Docker images** after code fixes | Docker layer cache | After EF/DI fixes, `docker compose up --build -d` (or rebuild affected services). Old `auth-api` image still lacked saga PK config. |

#### 1. PE-1 / PE-2 / PE-5 — Microservices, messaging, saga

**What was done:** Monolith split into Auth / Product / Cart / Order behind YARP; checkout orchestrated via MassTransit Saga + Outbox; email via MassTransit consumer in Order service; database-per-service (4 logical PostgreSQL databases).

**Pitfalls hit:**

| Pitfall | Cause | Solution |
|---|---|---|
| **`db-migrate` fails** with multiple DbContexts | EF Core `dotnet ef database update` without `--context` tries all contexts | `migrate-databases.sh`: `--context AppDbContext` + `AppDbContextFactory` for design-time |
| **Saga persistence error** — `OrderCheckoutState` has no primary key | MassTransit EF saga entity not configured | `AppDbContext`: `modelBuilder.Entity<OrderCheckoutState>().HasKey(x => x.CorrelationId)` |
| **Migrate startup project validation fails** | Using `Order.Api` as startup triggered `ValidateOnBuild` for all registered services | Migrate container uses **Core + ServiceDefaults only**; no full API startup |
| **`cart-api` / `order-api` crash at startup** — `Unable to resolve IPricingService` | `IPricingService` registered in `AddNovacartProduct()` only; Cart/Order call `AddNovacartInfrastructure()` | Register `IPricingService` in **shared infrastructure** (`NovacartServiceExtensions`) |
| **`Dockerfile.migrate` build fails** | Missing project reference copy | Copy `Novacart.ServiceDefaults` into migrate image |
| **Windows host port conflicts** on 9200 / 16686 | Local Elasticsearch or Jaeger already bound | Remove **host port mappings** for ES and Jaeger in `docker-compose.yml`; inter-container DNS only (`elasticsearch:9200`) |
| **Orphan monolith container** still running | Old `novacart-backend-1` from pre-split compose | `docker compose down --remove-orphans` |

**Lessons learned:**
- **Shared DI must live in shared extensions.** Anything Cart/Order needs (pricing, locks, outbox helpers) belongs in `AddNovacartInfrastructure`, not a single-service extension.
- **Saga entities need explicit EF keys** — MassTransit won't infer `CorrelationId` as PK.
- **Migration tooling should be minimal** — a dedicated migrate image avoids pulling entire service graphs into design-time validation.
- **PE-2 and PE-5 are not separate milestones** — track them under PE-1; RabbitMQ + Saga + Outbox ship together.
- **Verify the full compose stack**, not unit tests alone, after cross-service DI or EF model changes.

#### 2. PE-3 — ElasticSearch product search

**Stack:** Elasticsearch 8.15 · `Elastic.Clients.Elasticsearch` 8.15 · index `novacart-products` · Product API · optional `searchEngine` field on `PagedResult`.

**What was done:** Keyword search (`?q=`) routes to ES when healthy; admin/Square changes trigger index sync; startup reindex; automatic Postgres fallback.

**Pitfalls hit:**

| Pitfall | Cause | Solution |
|---|---|---|
| **C# client compile errors** | ES 8.x client API differs from legacy NEST | Use `Elastic.Clients.Elasticsearch`: `.Term(new TermsQueryField(...))`, `NumberRange`, union types for `.Match()` |
| **Indexer + DbContext lifetime** | Singleton indexer holding scoped `DbContext` | Register `ElasticProductSearchIndexer` as **Scoped** (or resolve via scope factory) |
| **InMemory EF can't test ILIKE fallback** | InMemory provider doesn't implement Postgres `ILIKE` | Test ES path with Testcontainers; test fallback in integration/Docker tests, not InMemory |
| **Testcontainers inside test Docker image** | Nested Docker / no socket | Lazy-init container; skip test when Docker unavailable (`DockerUnavailableException`) |
| **Assumed ES needs host port 9200** | Dev machines often already use 9200 | ES only on Docker network; gateway/product-api reach `http://elasticsearch:9200` internally |

**Lessons learned:**
- **Browse (no `q`) stays on Postgres** — ES is for keyword relevance; facets/sort unchanged on PG.
- **Always implement fallback** — set `searchEngine: "postgres"` so operators can see which path ran.
- **Read the new client docs** — `Elastic.Clients.Elasticsearch` is not a drop-in for NEST.
- See [docs/PE3-ELASTICSEARCH.md](docs/PE3-ELASTICSEARCH.md) for config and sync table.

#### 3. PE-4 — Distributed lock & inventory hardening ✅

**Stack:** Redis 7 · `RedisDistributedLockService` · `StockHoldService` + `stock_holds` table · `ProductStockRepository.TryDecrementStockAsync` · YARP rate limiting · [PE4-REDIS-HA.md](docs/PE4-REDIS-HA.md) · `StockInventoryMetrics` (OTel).

**What was done:** Baseline Redlock on `ReserveStockConsumer` **plus** production hardening — checkout holds at PaymentIntent creation, TTL expiry worker, conditional SQL decrement, gateway rate limit on `/api/checkout`, Redis HA connection-string docs, stock/lock OTel metrics.

**Pitfalls / misconceptions:**

| Pitfall | Cause | Solution |
|---|---|---|
| **Fake lock in unit tests always succeeds** | Test double doesn't simulate contention | Testcontainers Redis for concurrency tests |
| **"Redis lock = production-ready inventory"** | Lock alone doesn't cover reservation or DB races | PE-4 hardening adds holds + atomic SQL + rate limit (now ✅) |
| **Confused PE-6 with PE-4** | Redis cart cache vs stock lock | **PE-6** = cart reads; **PE-4** = inventory writes — separate |
| **Cart Redis vs stock Redis** | Same Redis instance, different keys/purpose | PE-6 uses `novacart:cart:*`; PE-4 uses `novacart:stock:*` — do not merge logic |

**Spring Cloud large mall — inventory stack (current state)**

| Capability | Typical Spring ecosystem | Novacart today |
|---|---|---|
| Stock deduct lock | Redisson | ✅ Redlock + HA config docs |
| Stock reservation (hold) | Redis / DB hold + TTL | ✅ `StockHoldService` |
| DB conditional update | `UPDATE … WHERE stock >=` | ✅ `ProductStockRepository` |
| Flash-sale rate limit | Sentinel + MQ | ✅ YARP fixed-window limiter |
| Shopping cart | Redis | ✅ PE-6 (Postgres truth + Redis snapshot) |
| Order sharding | ShardingSphere / custom | ✅ PE-7 UserId-hash pilot |

**Novacart-specific answers (post PE-4 ~ PE-7):**

| Question | Answer |
|---|---|
| Is **only** a Redis lock enough? | **No** — but PE-4 now adds holds, atomic SQL, and rate limiting. |
| Ready for **real flash sale / mega promo**? | **Much closer** — enable PE-4 stack + monitor OTel stock metrics; tune YARP limits per environment. |
| Does PE-6 replace PE-4? | **No** — cart cache does not reserve or deduct inventory. |
| Does PE-7 replace PE-4/PE-6? | **No** — sharding scales order **storage**; inventory and cart remain separate concerns. |

**Takeaway:** See [docs/PE4-PRODUCTION-HARDENING.md](docs/PE4-PRODUCTION-HARDENING.md).

#### 4. PE-5 — Admin saga visibility ✅

**What was done:** `CheckoutSagaAdminService` — list checkout sagas, retry failed steps; `RabbitMqMonitoringService.RetryErrorQueueAsync` for DLQ; Admin UI at `/admin/system`.

**Lesson:** PE-5 core flow ships with PE-1 Saga; the **admin retry UI** is the separate PE-5 deliverable for ops visibility.

#### 5. PE-6 — Redis cart cache ✅

**Stack:** `CartRedisStore` · `CartRedisSnapshot` · `ICartRedisStore` · `CartRedisOptions` (`Enabled`, guest/user TTL).

**What was done:** Read-through / write-through Redis snapshot per user/session; Postgres remains source of truth; prices/stock loaded from catalog on every DTO build; invalidate on clear/merge/checkout.

**Pitfalls:**

| Pitfall | Cause | Solution |
|---|---|---|
| **`Cart` namespace vs entity `Cart`** | `using Novacart.Api.Models.Dtos.Cart` conflicts with entity | Alias: `using CartEntity = Novacart.Api.Models.Entities.Cart` — **never** replace_all `Cart` in `CartService.cs` |
| **Caching prices in Redis** | Stale pricing if cached with line items | Only cache line items (productId + qty); resolve prices from catalog on read |

**Takeaway:** See [docs/PE6-REDIS-CART.md](docs/PE6-REDIS-CART.md). Default `CartRedis.Enabled=false`. **Follow-up ✅:** `CartRedisIntegrationTests` (Testcontainers Redis).

#### 6. PE-7 — Order SQL sharding pilot ✅

**Stack:** `OrderShardResolver` (FNV-1a mod shard count) · `order_shard_routes` · `IShardedOrderDb` · `novacart_commerce_0/1` · `OrderSharding.Enabled=false` default.

**What was done:** UserId-hash routing for orders/items/payments/status history; route table on legacy `novacart_commerce`; admin fan-out + legacy fallback for pre-sharding rows; webhook/saga lookup by `orderId` via route index.

**Pitfalls:**

| Pitfall | Cause | Solution |
|---|---|---|
| **`Guid.GetHashCode()` for sharding** | Not stable / can be negative | FNV-1a over Guid bytes + **unsigned** mod |
| **`replace_all Cart` in CartService** | Broke DTOs and method names | Only alias entity type; keep `ICartService` / `CartDto` names |
| **Admin pagination across shards** | No global ORDER BY in SQL | Fan-out + in-memory merge (pilot OK; OLAP later) |
| **Analytics double-count after backfill** | Legacy + shard both hold same order | Legacy query excludes IDs in `order_shard_routes` |
| **Backfill safety** | Accidental prod writes | CLI defaults to dry-run; `--apply` / `--delete-legacy` explicit |

**Optional follow-ups (2026-07-16) ✅:** `AnalyticsService` cross-shard aggregation · `OrderShardBackfillService` + `scripts/backfill-order-shards.sh` · `OrderShardingIntegrationTests`.

**Takeaway:** See [docs/PE7-SQL-SHARDING.md](docs/PE7-SQL-SHARDING.md). Enable only when order tables outgrow single Postgres.

#### 7. PE-8 — Thread pool tuning ✅

**Stack:** `ThreadPool.SetMinThreads` · optional `StripeWebhookWorkQueue` · OTel `Novacart.Runtime` / `Novacart.Webhook` · `dotnet-counters` script.

**What was done:** Configurable worker/IO thread floors at host startup; bounded webhook continuation queue on order-api (return 200 after idempotent persist); profiling script and deployment-guide values.

**Pitfalls:**

| Pitfall | Cause | Solution |
|---|---|---|
| **Raising min threads on tiny VMs** | Each thread ≈ 1 MB stack | Profile first; start with +CPU count, not 200 |
| **Webhook offload without idempotent log** | Double payment completion | Persist `payment_webhooks` before enqueue (unchanged) |
| **ServiceDefaults ↔ Core cycle** | Thread pool in shared project | Apply tuning in `MicroserviceBootstrap` / monolith `Program.cs`, not ServiceDefaults |

**Takeaway:** See [docs/PE8-THREAD-POOL.md](docs/PE8-THREAD-POOL.md). Default `ThreadPool.Enabled=false`.

#### 8. PE-10 — Internationalisation (i18n) ✅

**Stack:** `next-intl` · `/en/` + `/zh/` App Router segments · `messages/en.json` + `messages/zh.json` · `buildHreflangAlternates` · middleware `x-next-pathname`.

**What was done:** Customer shell + admin products/orders/pricing forms translated; `generateMetadata` emits canonical + `hreflang` (`en-AU`, `zh-CN`, `x-default`); `formatPrice` uses locale-aware Intl; unit tests in `seo.test.ts`.

**Pitfalls:**

| Pitfall | Cause | Solution |
|---|---|---|
| **Per-path hreflang from layout only** | Root layout lacks pathname | Middleware sets `x-next-pathname` for `generateMetadata` |
| **Wrong canonical in production** | Missing site URL | Set `NEXT_PUBLIC_SITE_URL` in production env |
| **API product names not translated** | Data stored as-is | Expected boundary — only UI chrome is i18n |

**Takeaway:** See [docs/PE10-I18N.md](docs/PE10-I18N.md). Analytics/system admin pages may retain English technical labels.

#### 9. PE-9 — AI chatbot ✅

**Stack:** `IChatCompletionClient` · OpenAI / Claude / Ollama · FAQ JSON · order context · `ChatWidget`.

**What was done:** Backend proxy with multi-provider switch; signed-in users get last 3 order summaries; PII redaction + opt-in; gateway rate limit on `/api/support/*`.

**Pitfalls:**

| Pitfall | Cause | Solution |
|---|---|---|
| **API keys in frontend** | Direct LLM calls from browser | Always proxy via `POST /api/support/chat` |
| **Claude system role** | Anthropic uses separate `system` field | Extract system message in `ClaudeChatClient` |
| **Guest 401 redirect** | `apiCall` bailToLogin | `optionalAuth: true` on chat endpoints |

**Takeaway:** See [docs/PE9-AI-CHATBOT.md](docs/PE9-AI-CHATBOT.md). Default `Chatbot.Enabled=false`; RAG optional follow-up.

#### 10. What "tests pass" still didn't prove (PE-1 ~ PE-10)

Same theme as the S3 session:

- **Unit tests green** did not catch Cart/Order startup DI failures — only **full compose up** did.
- **InMemory EF tests** did not validate Postgres `ILIKE` fallback or real ES queries.
- **Mock locks** did not prove concurrency — **Testcontainers Redis** did.
- **PE-6/PE-7 enabled paths** — covered by `CartRedisIntegrationTests` / `OrderShardingIntegrationTests` (Testcontainers / in-memory multi-DB); still run compose with flags enabled for staging acceptance.
- **HANDOFF smoke** (`GET /api/products?q=…` → `searchEngine: "elasticsearch"`) is the acceptance bar for PE-3.

**Rule of thumb:** After any PE touching **multiple containers**, **external infra** (ES, Redis, RabbitMQ), **shared DI**, or **multi-DB routing**, run `docker compose up --build -d`, check `db-migrate` exit 0, then hit the gateway — not just `dotnet test` on the host (use `Dockerfile.backend.test` when host lacks .NET 8).

---
> **⚠️ IMPORTANT: Always read the official documentation before using any technology.**

---

> **⚠️ IMPORTANT: Always read the official documentation before using any technology.**
> You don't need to finish the entire docs — start with the Quick Start / Tutorial, build a minimal example, then refer back as needed. Official docs are the single source of truth.

---

## Official Documentation Links

### Must-Read (Core Dependencies)

| Technology | Official Docs | What to Read First |
|---|---|---|
| **ASP.NET Core** | [learn.microsoft.com/aspnet/core](https://learn.microsoft.com/en-us/aspnet/core/) | Fundamentals → Middleware → DI → MVC |
| **Entity Framework Core** | [learn.microsoft.com/ef/core](https://learn.microsoft.com/en-us/ef/core/) | Getting Started → DbContext → Migrations → Querying |
| **Next.js** | [nextjs.org/docs](https://nextjs.org/docs) | App Router → Server Components → API Routes → Middleware |
| **TypeScript** | [typescriptlang.org/docs](https://www.typescriptlang.org/docs/) | Basic Types → Interfaces → Generics |
| **Stripe** | [stripe.com/docs](https://stripe.com/docs) | Payment Intents → Webhooks → Testing |
| **PostgreSQL** | [postgresql.org/docs](https://www.postgresql.org/docs/) | SQL Syntax → Data Types → Indexes |
| **Tailwind CSS** | [tailwindcss.com/docs](https://tailwindcss.com/docs) | Utility Classes → Responsive → Configuration |

### Should-Read (Important Supporting Tech)

| Technology | Official Docs | What to Read First |
|---|---|---|
| **Redis** | [redis.io/docs](https://redis.io/docs/) | Data Types → Commands → Pub/Sub |
| **Square API** | [developer.squareup.com/docs](https://developer.squareup.com/docs/) | Catalogue API → Sandbox → OAuth |
| **Docker** | [docs.docker.com](https://docs.docker.com/) | Get Started → Dockerfile → Compose |
| **AWS** | [docs.aws.amazon.com](https://docs.aws.amazon.com/) | EC2 → RDS → ElastiCache → S3 |
| **GitHub Actions** | [docs.github.com/actions](https://docs.github.com/en/actions) | Workflow Syntax → Jobs → Triggers |
| **Swashbuckle (Swagger)** | [github.com/domaindrivendev/Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore) | README → Getting Started |

### Quick Reference (Look Up When Needed)

| Technology | Official Docs |
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

## Architecture Decisions

### Monolithic MVP vs Microservices

| Aspect | Monolithic MVP | Microservices |
|---|---|---|
| Project count | 1 application | 5+ independent services |
| Database | 1 shared database | Each service owns its own DB |
| Deployment | 1 Docker container | N containers + gateway + service discovery |
| Dev time | Weeks | Months |
| Team size | 1-2 people | 5+ people |
| Suitable for | MVP, capstone projects | Large-scale production systems |

**Decision: Start with Monolithic MVP (MonolithFirst pattern)**

- Martin Fowler's recommendation: build monolith first, decompose later
- Key preparation for future decomposition:
  - Clean layered architecture (Controller → Service → Mapper)
  - Module boundaries (Auth, Product, Cart, Order code separated)
  - Database tables per module (no cross-module JOINs)
  - RESTful API design (frontend doesn't need changes when splitting)

### Layered Architecture

```
Controller (HTTP) → Service (Business Logic) → Mapper/Repository (Data Access) → Entity (Domain Model)
```

- **Controller**: Handles HTTP requests, validates input, returns responses
- **Service**: Contains business logic, orchestrates operations
- **Mapper/Repository**: Data access layer, SQL queries, ORM operations
- **Entity**: Domain models, database table mappings

---

## Backend Technologies

### ASP.NET Core vs Spring Boot

| Aspect | ASP.NET Core (.NET) | Spring Boot (Java) |
|---|---|---|
| Language | C# | Java |
| ORM | Entity Framework Core | MyBatis Plus / JPA |
| DI | Built-in | Spring IoC Container |
| Performance | Excellent (Kestrel) | Excellent (Tomcat/Netty) |
| Cross-platform | Yes (.NET 8+) | Yes (JVM) |

### Entity Framework Core (EF Core)

.NET's official ORM, equivalent to MyBatis Plus in Java ecosystem.

| MyBatis Plus (Java) | EF Core (.NET) |
|---|---|
| SQL mapping, XML/annotations | Code-First, auto-generate tables from C# classes |
| `BaseMapper<T>` for CRUD | `DbContext` + LINQ for CRUD |
| Lambda QueryWrapper | LINQ query expressions |
| Code Generator | EF Core Migrations |
| Manual SQL for complex queries | LINQ sufficient for most cases; raw SQL available |

**Key concepts:**
- **Code-First**: Define C# classes → EF generates database schema
- **Migrations**: Version control for database schema changes (`dotnet ef migrations add`, `dotnet ef database update`)
- **DbContext**: Main class for database interaction, equivalent to MyBatis Mapper interface

### Swagger / OpenAPI

Auto-generated interactive API documentation.

- Generated from controller annotations/decorators
- Frontend developers can explore, test, and integrate APIs without manual docs
- Single source of truth for contract-first development
- ASP.NET Core: Swashbuckle NuGet package

### Unified API Layer (Frontend)

A reusable `apiCall` wrapper around Fetch API:
- Auto-injects JWT tokens into request headers
- Parses 3 error formats (JSON, text, network)
- Triggers auto-logout on 401/403 responses
- Eliminates boilerplate across 22+ frontend API calls

---

## Frontend Technologies

### Next.js vs CRA vs Vite

| Tool | Status | Use Case |
|---|---|---|
| **Next.js** | ✅ Current standard | SSR/SSG, full-stack React apps |
| **CRA** | ❌ Deprecated | Legacy projects only |
| **Vite** | ✅ Modern | SPAs, library development |

**Next.js advantages:**
- Built-in App Router (no React Router needed)
- Server-side rendering (SSR) and static generation (SSG)
- API Routes (backend endpoints in same project)
- Turbopack/Webpack built-in (no Vite needed)
- Image optimisation, code splitting out of the box

### React Virtual DOM & Diff Algorithm

**Why React is fast:**
1. Maintains a lightweight copy of the real DOM (Virtual DOM)
2. When state changes, creates a new Virtual DOM tree
3. Diffs the new tree against the old one (Reconciliation)
4. Only updates the actual DOM nodes that changed
5. Batch updates for minimal reflows/repaints

### Custom Hooks vs HOC (Higher-Order Components)

| Aspect | Custom Hooks | HOC |
|---|---|---|
| Composition | Clean, composable | Wrapper hell (deep nesting) |
| TypeScript | Excellent inference | Complex generic types |
| Naming | `useAuth`, `useCart` | `withAuth(Component)` |
| React recommendation | ✅ Preferred | ⚠️ Legacy pattern |
| Debugging | Clear component tree | Confusing wrapped components |

**Custom Hooks examples:**
- `useAuth()` — authentication state and methods
- `useCart()` — cart operations (add, remove, update)
- `useApi()` — API calls with loading/error states

### Reusable Component Design

Extract shared components to reduce code duplication (~40% reduction):
- **Layout**: Sidebar, Header, Footer
- **Business**: ProductCard, CartItem, OrderRow
- **UI**: Button, Input, Modal, Toast

---

## Database & Caching

### PostgreSQL vs MySQL

Both are relational databases; PostgreSQL offers:
- Better JSON support (JSONB)
- More advanced indexing (GIN, GiST)
- Stricter ACID compliance
- Better concurrency handling (MVCC)

### Redis Use Cases in E-commerce

| Use Case | Description |
|---|---|
| Session storage | Store user sessions for fast retrieval |
| Cart caching | Shopping cart data for sub-millisecond reads |
| Order cache | Recent orders to avoid repeated DB queries |
| JWT blacklist | Invalidated tokens (logout before expiry) |
| Rate limiting | Request counting per IP/user |
| Distributed lock | Redlock for inventory deduction atomicity |

### Alibaba Development Standards (Database)

- **Index naming**: `idx_table_column` (e.g., `idx_order_user_id`)
- **Field types**: `BIGINT` for IDs, `DECIMAL` for money (never FLOAT)
- **SQL guidelines**: Avoid `SELECT *`, use explicit column lists
- **Naming**: snake_case for tables and columns

### SQL Sharding (Planned)

Horizontal partitioning of large tables:
- **By date**: Orders table split by month/year
- **By user ID**: Hash-based partitioning
- **When to use**: Single table > 10M rows, query performance degrades

---

## Authentication & Security

### JWT (JSON Web Token) Implementation

**Structure:** `Header.Payload.Signature`

**Details:**
- **Algorithm**: HS256 (HMAC-SHA256) with server-side secret key
- **Payload**: user ID, email, role claims
- **Expiry**: Short-lived access token (default 15 min) + long-lived refresh token (7 days) — see Refresh Tokens below
- **Extraction**: `Authorization: Bearer <token>` header, or automatically from the `novacart_jwt` HttpOnly cookie via `OnMessageReceived`

### Token Storage Security

| Method | Security | Recommendation |
|---|---|---|
| **HttpOnly Cookie** | ✅ JS cannot access, prevents XSS | **Recommended** |
| **Memory (React State)** | ✅ Lost on page refresh, most secure | Use with Refresh Token |
| **localStorage** | ❌ XSS can read directly | **Not recommended** |
| **sessionStorage** | ❌ XSS can read directly | **Not recommended** |

**HttpOnly Cookie flags:**
- `HttpOnly` — JavaScript cannot access
- `Secure` — HTTPS only in production
- `SameSite=Strict` — Prevents CSRF attacks

### Refresh Tokens (Access + Refresh Pair)

Short-lived access tokens limit the blast radius of a stolen token, but force frequent re-authentication. The refresh-token pattern keeps UX smooth while staying secure.

**Two-token model:**
| Token | Lifetime | Stored in | Purpose |
|---|---|---|---|
| **Access token** (JWT) | 15 min | `novacart_jwt` cookie, `Path=/api` | Authorises every API request |
| **Refresh token** (opaque) | 7 days | `novacart_refresh` cookie, `Path=/api/auth` | Exchanged for a new access token via `POST /api/auth/refresh` |

**Key practices:**
- **Refresh tokens are opaque + hashed.** The DB stores only a SHA-256 hash (`refresh_tokens` table); the raw value is returned to the cookie once. Unlike passwords, no slow KDF (bcrypt) is needed — the token is already 256-bit random.
- **Rotation on every refresh.** Each refresh revokes the old token and issues a new pair (`ReplacedByTokenHash` links them). A token can only be used once.
- **Reuse detection.** If a *revoked* token is presented again, assume compromise and revoke the entire family (all tokens for that user). This is the main security benefit of rotation.
- **Narrow refresh-cookie path.** `Path=/api/auth` means the refresh cookie is sent only to auth endpoints — never to product/cart/order requests, minimising its exposure.
- **Frontend coalescing.** When the access token expires, many concurrent requests 401 at once. A module-level singleton `refreshPromise` collapses them into a single refresh call; otherwise N parallel refreshes would each rotate and invalidate the others.

### Role-Based Access Control (RBAC)

- Three roles: Customer, Administrator, System Administrator
- Role claims embedded in JWT token
- Frontend: route guards redirect unauthorised users
- Backend: endpoint authorisation rejects non-admin tokens (403 Forbidden)

### Password Security

- **Hashing**: bcrypt (salted, one-way hash)
- **Password reset flow**: Request → Generate time-limited token → Email → Submit new password with token → Validate & update

### Form Validation

- Real-time client-side validation
- Dynamic button state (disabled until form valid)
- Enter-key submission support
- Server-side validation as second layer of defence

---

## Message Queues & Async Processing

### RabbitMQ vs Kafka vs RocketMQ

| Feature | RabbitMQ | Kafka | RocketMQ |
|---|---|---|---|
| Model | Queue-based | Log-based | Queue-based |
| Throughput | Medium | Very high | High |
| Use case | Task queues, RPC | Event streaming, logs | E-commerce (Alibaba) |
| Complexity | Low | Medium | Medium |
| .NET support | ✅ Official client | ✅ Confluent client | ⚠️ Limited |

**RabbitMQ in e-commerce:**
- Async order processing (payment → inventory → email)
- Email notification queue
- Inventory update queue

### When to Use Message Queues

- ✅ Multiple steps after checkout (payment, inventory, email, warehouse)
- ✅ High volume traffic (flash sales)
- ✅ Cross-service communication in microservices
- ❌ Simple synchronous operations (MVP stage)
- ❌ Single-service architecture

### In-Process Queues with `Channel<T>` (What Novacart Uses)

Before adopting a real broker (RabbitMQ), Novacart decouples slow side-effects (email) from request handling using .NET's built-in `System.Threading.Channels` — a lightweight, in-process producer/consumer queue.

**How it fits:**
```
Request handler ──enqueue──▶ Channel<EmailMessage> (bounded) ──▶ BackgroundService worker ──▶ SMTP
  (returns immediately)            (back-pressure)              (own DI scope per message)
```

**`Channel<T>` vs RabbitMQ — when to use which:**
| | `Channel<T>` (in-process) | RabbitMQ (broker) |
|---|---|---|
| Scope | Single process | Cross-process / cross-service |
| Persistence | ❌ Lost on restart | ✅ Survives restart (durable queues) |
| Setup cost | Zero (built into .NET) | Infrastructure (container + config) |
| Back-pressure | ✅ `BoundedChannelFullMode.Wait` | ✅ QoS / prefetch |
| Use when | Decoupling within one monolith | Distributing work across services |

**Novacart's choice:** The email queue is in-process because Novacart is a monolith — there's only one producer and one consumer in the same process. If the app splits into services later, swapping `Channel<T>` for a RabbitMQ consumer is a localised change. See the [Practice Log](#2026-0715-p2p3-features--jwt-refresh-async-email-s3-and-the-bugs-they-surfaced) for the scope-lifetime pitfall this surfaced.

---

## Search Engine

### ElasticSearch

Full-text search engine, complements SQL databases.

**When to use:**
- Product catalogue > 10,000 items
- Complex faceted search (material, style, price range)
- Full-text keyword search across multiple fields
- SQL `LIKE` queries become too slow

**Not needed when:**
- Product data comes from external API (Square)
- Small dataset (< 1,000 items)
- Simple filtering is sufficient

---

## Design Patterns

### Strategy Pattern

**Use case**: Interchangeable payment providers (Stripe, PayPal, future gateways)

```
IPaymentStrategy (interface)
├── StripePaymentStrategy
├── PayPalPaymentStrategy
└── AlipayPaymentStrategy

PaymentContext → uses IPaymentStrategy
```

- Open/Closed principle: add new payment methods without modifying existing code
- Client code depends on interface, not concrete implementation

### Factory Pattern

**Use case**: Object creation logic (payment strategy creation, notification creation)

```
PaymentFactory.Create("stripe") → StripePaymentStrategy
PaymentFactory.Create("paypal") → PayPalPaymentStrategy
```

### Other Common Patterns

| Pattern | Use Case |
|---|---|
| Singleton | Database connection, configuration |
| Observer | Event-driven updates |
| Builder | Complex object construction (e.g., query builders) |
| Repository | Data access abstraction |

---

## Testing

### Backend: xUnit (.NET)

- Industry-standard .NET testing framework
- Unit tests: test individual methods/functions
- Integration tests: test API endpoints end-to-end
- Runs automatically in CI pipeline on every commit

```csharp
[Fact]
public void CalculateTotal_ReturnsCorrectSum()
{
    var cart = new Cart { Items = new[] { new Item { Price = 10, Qty = 2 } } };
    Assert.Equal(20, cart.CalculateTotal());
}
```

### Frontend: Vitest + React Testing Library

| Tool | Purpose |
|---|---|
| **Vitest** | Fast unit tests with native ESM support |
| **React Testing Library** | Component testing from user's perspective |

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

## DevOps & Deployment

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

### AWS Architecture

```
Users → EC2 (App) → RDS (PostgreSQL)
                  → ElastiCache (Redis)
                  → S3 (Static Assets)
```

### CI/CD with GitHub Actions

- Auto-build on push
- Run tests (xUnit + Vitest)
- Docker image build and push
- Deploy to AWS

---

## Performance Optimisation

### Intersection Observer (Lazy Loading)

```javascript
// Lazy load images — only load when entering viewport
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

**Benefits:**
- Reduces initial page load time
- Saves bandwidth (images below the fold not loaded)
- Improves Core Web Vitals (LCP, FID)

### Thread Pool Tuning

- ASP.NET Core uses `Task` and built-in thread pool
- Custom configuration via `ThreadPool.SetMinThreads` for high-concurrency scenarios
- Use cases: flash sales, bulk order processing

### Code Splitting

- Next.js App Router: automatic code splitting per route
- Dynamic imports: `const Component = dynamic(() => import('./Component'))`
- Reduces initial bundle size

---

## CSS & Responsive Design

### CSS Grid Adaptive Layout

```css
.product-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);  /* Desktop */
  gap: var(--section-gap);
}

@media (max-width: 1200px) {
  .product-grid { grid-template-columns: repeat(3, 1fr); }  /* Tablet landscape */
}

@media (max-width: 900px) {
  .product-grid { grid-template-columns: repeat(2, 1fr); }  /* Tablet portrait */
}

@media (max-width: 600px) {
  .product-grid { grid-template-columns: 1fr; }  /* Mobile */
}
```

### CSS Custom Properties

```css
:root {
  --container-max: 1200px;
  --section-gap: 1.5rem;
  --header-height: 4rem;
}
```

### rem vs px vs em

| Unit | Description | Use Case |
|---|---|---|
| **rem** | Relative to root font-size (16px default) | ✅ Font sizes, spacing |
| **px** | Absolute pixels | ❌ Avoid for responsive |
| **em** | Relative to parent font-size | Component-level scaling |

### Responsive Sidebar (Auto-collapse)

```javascript
// Auto-collapse sidebar at ≤768px
const mediaQuery = window.matchMedia('(max-width: 768px)');
mediaQuery.addEventListener('change', (e) => {
  setSidebarCollapsed(e.matches);
});
```

---

## PWA (Progressive Web App)

### Core Components

| Component | File | Purpose |
|---|---|---|
| **Web App Manifest** | `manifest.json` | App name, icons, theme colour, display mode |
| **Service Worker** | `sw.js` | Offline caching, background sync, push notifications |
| **Installable** | Browser prompt | Add to home screen |

### Service Worker Lifecycle

```
Register → Install → Activate → Fetch (intercept network requests)
```

### manifest.json Example

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

## Payment Integration

### Stripe Sandbox

- **Test mode**: No real charges, use test card numbers
- **Test card**: `4242 4242 4242 4242`, any future expiry, any CVC
- **Payment flow**: Client creates payment intent → Server confirms → Webhook notifies result

### Stripe Webhooks

- Stripe sends POST request to your endpoint on payment events
- Events: `payment_intent.succeeded`, `payment_intent.failed`
- **ngrok**: Exposes local server to internet for webhook testing

```bash
# Start ngrok tunnel
ngrok http 5000

# Forward Stripe events
stripe listen --forward-to localhost:5000/api/webhooks/stripe
```

### Payment Tokenisation

- Card details never touch your server
- Stripe.js creates a token on the client
- Server receives only the token, not the card number
- PCI compliance simplified

---

## Object Storage (S3)

### Presigned URLs — Upload Direct to S3

Instead of proxying file uploads through the backend (which buffers the whole file in memory), the backend issues a **presigned URL** and the browser uploads directly to S3.

**Flow:**
```
Browser ──POST /api/admin/uploads/presign──▶ Backend ──▶ { uploadUrl, publicUrl }
Browser ──PUT uploadUrl (file body)──────────▶ S3
Browser stores publicUrl as product.ImageUrl
```

**Why presigned:**
- Backend never handles file bytes → no memory/CPU pressure
- Uploads go straight to S3's CDN edge → faster for large files
- URLs expire (e.g. 5 min) → limited reuse window

**Config-driven provider switching (LocalStack ↔ AWS):**
```csharp
// ServiceUrl set → LocalStack / S3-compatible (dev, no AWS account)
// ServiceUrl unset → real AWS via default credential chain (prod)
```

### LocalStack — S3 Without an AWS Account

[LocalStack](https://localstack.local/) emulates AWS services locally. In dev, `docker-compose` runs `localstack/localstack` so the S3 code path is fully exercised without AWS credentials or billing.

**Two gotchas (caught only in end-to-end testing):**
1. **Buckets aren't auto-created** in LocalStack 3.x despite the `BUCKETS` env var. Fix: mount an init script to `/etc/localstack/init/ready.d/` that runs `awslocal s3 mb`.
2. **Presigned URLs default to `https://`** but LocalStack serves plain HTTP. Fix: when `ServiceUrl` is `http://`, post-process the generated URL to force `http://`.

> See the [Practice Log entry](#2026-0715-p2p3-features--jwt-refresh-async-email-s3-and-the-bugs-they-surfaced) for the full debugging story.

---

## Microservices Concepts

### .NET Microservice Equivalents

| Spring Cloud (Java) | .NET Alternative |
|---|---|
| Nacos (Registry) | Consul / .NET Aspire |
| Nacos (Config) | .NET Configuration + Consul KV |
| Spring Cloud Gateway | YARP / Ocelot |
| Sentinel (Circuit Breaker) | Polly |
| OpenFeign (HTTP Client) | HttpClientFactory + Refit |
| Seata (Distributed Tx) | Saga pattern + MassTransit |
| Spring Cloud Stream | MassTransit (RabbitMQ/Kafka) |
| Sleuth + Zipkin | OpenTelemetry + Jaeger |

### API Gateway

- **Ocelot**: .NET API gateway (routing, rate limiting, auth)
- **YARP**: Microsoft's reverse proxy (high performance)
- **Purpose**: Unified entry point, hide internal service topology

### Service Discovery

- **Consul**: HashiCorp's service discovery + config
- **.NET Aspire**: Microsoft's cloud-native stack (newer)

---

## Distributed Transactions

### ACID vs BASE

| ACID (Traditional) | BASE (Distributed) |
|---|---|
| Atomicity | Basically Available |
| Consistency | Soft state |
| Isolation | Eventually consistent |
| Durability | |

### Seata Modes

| Mode | How It Works | Pros | Cons |
|---|---|---|---|
| **AT** | Auto-generates undo_log, auto-rollback on failure | Simple, no manual rollback code | Performance overhead (extra write) |
| **TCC** | Manual Try/Confirm/Cancel methods | High performance, fine-grained control | High dev cost |
| **Saga** | Each step has a compensation action, reverse order on failure | Good for long transactions | Eventual consistency, delay |

**AT Mode flow:**
1. Begin global transaction
2. Each local transaction writes undo_log (before-image + after-image)
3. If all succeed → commit, delete undo_log
4. If any fail → execute undo_log to rollback

**When needed:** Multiple services, multiple databases only.
**For single DB:** Use local transactions (BEGIN/COMMIT/ROLLBACK).

---

## Interview Cheat Sheet

### Why React?

- Virtual DOM + Diff algorithm → only re-renders changed components
- Component-based architecture → reusable, testable, composable
- Unidirectional data flow → predictable state management
- Huge ecosystem (Next.js, React Native, etc.)

### Why Next.js over CRA?

- CRA is deprecated; Next.js is the React team's recommended framework
- Built-in SSR/SSG (better SEO, faster initial load)
- App Router with file-based routing (no React Router needed)
- API Routes (backend in same project)
- Image optimisation, code splitting out of the box

### Why PostgreSQL over MySQL?

- Better JSON support (JSONB)
- Advanced indexing (GIN, GiST)
- Stricter ACID compliance
- Better concurrency (MVCC)

### Why EF Core over Dapper?

| EF Core | Dapper |
|---|---|
| Full ORM, Code-First | Micro-ORM, SQL-first |
| LINQ queries | Raw SQL |
| Migrations built-in | Manual schema management |
| Slower (overhead) | Faster (direct SQL) |
| More features | More control |

### Where to store JWT?

**HttpOnly Cookie** — prevents XSS from stealing tokens. Never localStorage/sessionStorage.

### MonolithFirst

Build monolith → modularise → extract services when needed. Don't start with microservices.

### Strategy vs Factory Pattern

- **Strategy**: Choose algorithm at runtime (payment methods)
- **Factory**: Create objects without specifying exact class (payment strategy creation)

---

## Glossary

| Term | Definition |
|---|---|
| **JWT** | JSON Web Token — stateless authentication token |
| **RBAC** | Role-Based Access Control |
| **ORM** | Object-Relational Mapping (EF Core, MyBatis Plus) |
| **PWA** | Progressive Web App — installable, offline-capable web app |
| **SSR** | Server-Side Rendering |
| **SSG** | Static Site Generation |
| **SPA** | Single Page Application |
| **CQRS** | Command Query Responsibility Segregation |
| **ACID** | Atomicity, Consistency, Isolation, Durability |
| **MVCC** | Multi-Version Concurrency Control |
| **CORS** | Cross-Origin Resource Sharing |
| **CSRF** | Cross-Site Request Forgery |
| **XSS** | Cross-Site Scripting |
| **PCI** | Payment Card Industry (compliance standard) |
| **Redlock** | Redis distributed locking algorithm |
| **AT Mode** | Automatic Transaction mode in Seata |
| **TCC** | Try-Confirm-Cancel distributed transaction pattern |
| **Saga** | Long-running transaction with compensating actions |
