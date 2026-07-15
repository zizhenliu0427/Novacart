# PE-1 Microservices — Novacart Final Architecture

> **Status:** **Implemented** — default `docker compose up` runs the microservices stack. Legacy monolith: `docker-compose.monolith.yml`. Design reference for the original monolith: [ARCHITECTURE.md](ARCHITECTURE.md).
>
> This is the **single canonical microservices + async orchestration design** for Novacart. It supersedes the earlier Consul/Ocelot bullet in README.
>
> See also: [README §Planned Enhancements](../README.md#planned-enhancements) · [HANDOFF §11](../HANDOFF.md#11-planned-enhancements-scaling-tail--not-scheduled) · [TODO.md PE-1](../TODO.md#pe-1--microservice-architecture) · [中文版](MICROSERVICES-PE1_ZH.md)

---

## 1. Final stack

| Layer | Choice | Role |
|-------|--------|------|
| Orchestration / local DX | **[.NET Aspire](https://learn.microsoft.com/dotnet/aspire/)** AppHost | Multi-service dev, config, dashboard, OpenTelemetry |
| API gateway | **[YARP](https://microsoft.github.io/reverse-proxy/)** | Single `/api` entry, routing, auth |
| Service discovery | **Aspire Service Discovery** → **Kubernetes** in prod | No Consul required |
| Sync resilience | **Polly** + `HttpClientFactory` | Retry, circuit breaker, timeout |
| HTTP clients | **Refit** | Typed inter-service calls |
| Message bus | **RabbitMQ** | Async, decoupling, peak shaving |
| Messaging framework | **[MassTransit](https://masstransit.io/)** | Consumers, retries, DLQ, saga state machines |
| Reliable publish | **Transactional Outbox** (MassTransit EF) | Same DB transaction as business write, then dispatch to MQ |
| Distributed checkout | **MassTransit Saga** (Order service) | Payment → inventory → email → clear cart |
| Inventory concurrency | **Redis Redlock** (PE-4, same rollout) | Multi-instance stock decrement |
| Observability | OpenTelemetry (Aspire defaults) | Tracing |
| Deploy | Docker → **Kubernetes** / ECS | Aspire manifests or Helm · see [DATABASE-PER-SERVICE.md](DATABASE-PER-SERVICE.md) |

**Not used:** Consul / Ocelot classic stack (retained only for Spring Cloud comparison in §6).

**PE-2 (RabbitMQ) and PE-5 (async order processing) are folded into this design** — not parallel optional tracks.

---

## 2. Service boundaries

| Service | Responsibility | Database |
|---------|----------------|----------|
| **Auth** | Register/login, JWT, refresh tokens, roles | `users`, `roles`, `user_roles`, `refresh_tokens` |
| **Product** | Catalogue, search, pricing rules, Square sync, **stock decrement API** | `categories`, `products`, `price_rules` |
| **Cart** | Cart CRUD, guest merge | `carts`, `cart_items` |
| **Order** | Checkout, Stripe webhooks, status machine, **saga orchestration**, outbox | `orders`, `order_items`, `payments`, outbox, saga state |

Gateway routes: `/api/auth/**` → Auth; `/api/products/**` → Product; `/api/cart/**` → Cart; `/api/checkout/**`, `/api/orders/**` → Order.

The Next.js app keeps calling `/api/*`.

---

## 3. MassTransit + RabbitMQ + Saga + Outbox

### Why

| Monolith today | After split | Pattern |
|----------------|-------------|---------|
| Single DB transaction on checkout | No cross-service ACID | **Saga** (eventual consistency) |
| In-process `EmailQueue` | Lost messages across replicas | **MassTransit** + RabbitMQ |
| Update DB then publish (risky) | Partial failure | **Transactional Outbox** |

### Transactional Outbox

Inside **Order service**, one PostgreSQL transaction:

1. Update order / payment state  
2. Insert MassTransit **outbox** row  
3. Commit  
4. Outbox dispatcher publishes to RabbitMQ  

Consumers must be **idempotent** (`orderId`, Stripe `event_id`).

### Checkout saga (MassTransit state machine)

1. Client → Order: create pending order + Stripe session  
2. Stripe webhook → Order: commit paid state + outbox **`PaymentCompleted`**  
3. Product consumer: decrement stock (Redlock + idempotency) → **`StockReserved`** or **`StockReservationFailed`**  
4. Saga: on success → outbox **`OrderPaid`** → email consumer; **`ClearCartForOrder`** → Cart consumer  
5. On stock failure → compensate (cancel order + **Stripe refund** via `IStripeRefundService`)

Replaces today’s single-transaction path in `PaymentService.HandleWebhookAsync`, in-process `EmailQueue`, and inline cart clear.

---

## 4. Spring Cloud comparison (final Novacart row)

| Capability | Spring Cloud | **Novacart final** |
|------------|--------------|-------------------|
| Framework | Spring Boot | ASP.NET Core 8+ |
| Cloud-native DX | Spring Cloud K8s / manual | **.NET Aspire** |
| Discovery | Nacos / Eureka | **Aspire / K8s DNS** |
| Gateway | Spring Cloud Gateway | **YARP** |
| Circuit breaker | Sentinel | **Polly** |
| Messaging | Spring Cloud Stream | **MassTransit + RabbitMQ** |
| Reliable publish | Custom / broker TX | **MassTransit EF Outbox** |
| Long-running flow | Seata / custom | **MassTransit Saga** |
| Tracing | Sleuth + Zipkin | **OpenTelemetry** |

No **Seata** — use **Saga + Outbox** for eventual consistency.

---

## 5. Rollout phases

1. Aspire AppHost + RabbitMQ + YARP skeleton  
2. Order: Outbox + `PaymentCompleted` (can validate before full split)  
3. Product: stock consumer + Redlock  
4. Cart: clear-cart consumer  
5. MassTransit saga + email consumer  
6. Auth split; K8s ingress/secrets; DLQ monitoring ✅  
7. OpenTelemetry (Jaeger OTLP); MassTransit email queue; Testcontainers ✅  
8. Refit catalog client; AppHost 4 DB parity; prod/K8s manifests ✅  

**Message + Outbox before** splitting all four databases.

---

## References

- [MassTransit — Transactional Outbox](https://masstransit.io/documentation/patterns/transactional-outbox)
- [MassTransit — Saga State Machines](https://masstransit.io/documentation/patterns/saga/state-machine)
- [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/)

| Date | Change |
|------|--------|
| 2026-07-15 | Final: Aspire + YARP + MassTransit + RabbitMQ + Saga + Outbox; PE-2/PE-5 merged |
