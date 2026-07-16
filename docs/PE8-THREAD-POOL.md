# PE-8 — Thread pool tuning (checkout / webhook burst)

> **Status:** Implemented (2026-07-16). **Disabled by default** — set `ThreadPool__Enabled=true` when profiling shows pool starvation.

## Problem

Under flash-sale checkout or Stripe webhook bursts, the CLR thread pool may grow worker threads slowly (default min ≈ CPU count), causing tail latency on:

- `POST /api/checkout/webhook/stripe` (order-api)
- `POST /api/checkout` session creation
- MassTransit consumers sharing the same process

## Design

| Component | Role |
|-----------|------|
| **`ThreadPoolTuningOptions`** | Config section `ThreadPool` — min worker / IO threads |
| **`ThreadPoolTuningApplicator`** | Calls `ThreadPool.SetMinThreads` at host startup |
| **`ThreadPoolRuntimeMetrics`** | OTel meter `Novacart.Runtime` — available workers, pending work |
| **`StripeWebhookWorkQueue`** | Optional bounded channel — return HTTP 200 after idempotent persist |
| **`StripeWebhookBackgroundWorker`** | N parallel consumers on dedicated long-running tasks |

MassTransit saga processing remains the primary async path when RabbitMQ is enabled; webhook offload shortens **HTTP thread** occupancy only.

## Configuration (order-api / monolith)

```json
"ThreadPool": {
  "Enabled": false,
  "MinWorkerThreads": 50,
  "MinCompletionPortThreads": 50,
  "OffloadStripeWebhooks": false,
  "WebhookWorkerCount": 2,
  "WebhookQueueCapacity": 256
}
```

Enable in Docker (order-api only recommended):

```yaml
ThreadPool__Enabled: "true"
ThreadPool__MinWorkerThreads: "50"
ThreadPool__MinCompletionPortThreads: "50"
ThreadPool__OffloadStripeWebhooks: "true"
ThreadPool__WebhookWorkerCount: "4"
```

**Defaults stay off** — tune per environment after load test.

## Profiling workflow

1. Reproduce burst (k6, Stripe CLI replay, or parallel checkout smoke).
2. Run `backend/scripts/profile-threadpool.sh order-api`.
3. Watch `System.Runtime` counters: `threadpool-thread-count`, `threadpool-queue-length`.
4. Watch `Novacart.Runtime` / `Novacart.Webhook` meters in Jaeger/Prometheus if exported.
5. Raise min threads until queue length stays near zero under peak; avoid over-provisioning idle threads in small dev VMs.

### dotnet-counters (manual)

```bash
dotnet-counters ps
dotnet-counters monitor --process-id <PID> \
  --counters System.Runtime[threadpool-thread-count,threadpool-queue-length]
```

### Application Insights

When Azure Monitor exporter is enabled, runtime + custom meters appear alongside ASP.NET request duration — correlate webhook P99 with `threadpool-queue-length`.

## Webhook offload semantics

When `OffloadStripeWebhooks=true`:

1. Verify Stripe signature (request thread).
2. Insert `payment_webhooks` row idempotently (request thread).
3. Enqueue continuation → **return 200 to Stripe**.
4. Background worker runs payment completion / hold release in a **new DI scope**.

Idempotency is preserved via `payment_webhooks.event_id` unique index and `Processed` flag.

## Boundaries

- Does **not** replace MassTransit or a separate webhook worker **process** — document that as a future scale step if order-api CPU is saturated.
- Gateway / product-api / cart-api inherit min-thread tuning via `ConfigureNovacartThreadPool()` but webhook queue registers **only** on order paths.
- Tune min threads conservatively on `t3.small`-class hosts (memory ∝ thread stacks).

## Related

- [TODO.md § PE-8](../TODO.md#pe-8--thread-pool-tuning)
- [deployment-guide.md](deployment-guide.md) — per-environment values
- `Novacart.Core/Infrastructure/ThreadPool/`
