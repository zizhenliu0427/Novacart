# PE-4 — Redis high availability

> Dev/staging uses a single `redis:7-alpine` container. Production should use **Sentinel** or **Cluster** so lock and cache paths survive node failure.

## Connection configuration

Novacart reads Redis via **`Redis:Configuration`** (StackExchange.Redis format) or fallback **`ConnectionStrings:Redis`**.

```json
"ConnectionStrings": {
  "Redis": "localhost:6379"
},
"Redis": {
  "Configuration": "localhost:6379"
}
```

### Sentinel (recommended for locks + cache)

```json
"Redis": {
  "Configuration": "sentinel1:26379,sentinel2:26379,sentinel3:26379,serviceName=mymaster,abortConnect=false"
}
```

- Deploy 3+ Sentinel processes monitoring one primary + replicas.
- Use **`abortConnect=false`** so the app retries during failover.
- For **full Redlock quorum**, point `RedisDistributedLockService` at **multiple independent Redis masters** (advanced; not enabled in dev).

### Cluster (throughput-first)

```json
"Redis": {
  "Configuration": "node1:6379,node2:6379,node3:6379,abortConnect=false"
}
```

- Suitable for cache-heavy workloads; verify lock key hashing stays on one slot if using single-key locks (Novacart uses one key per product — OK).

## Docker Compose (dev)

Default `docker-compose.yml` keeps a **single master** with healthcheck — sufficient for PE-4 feature development.

For production, replace the `redis` service with managed ElastiCache / Azure Cache / self-hosted Sentinel — update each API’s `ConnectionStrings__Redis` or `Redis__Configuration`.

## Environment examples

`.env.prod.example`:

```bash
# Single primary (minimal prod)
ConnectionStrings__Redis=redis-primary.internal:6379

# Sentinel
Redis__Configuration=sentinel1:26379,sentinel2:26379,sentinel3:26379,serviceName=novacart,abortConnect=false
```

## Failover behaviour

- **Locks:** TTL (30s) prevents deadlocks if a holder crashes; after failover, new acquirers retry.
- **Holds:** Backed by PostgreSQL `stock_holds` — not lost on Redis failover.
- **Cache:** Best-effort; cold cache after failover is acceptable.

## Related

- [PE4-PRODUCTION-HARDENING.md](PE4-PRODUCTION-HARDENING.md)
- [StackExchange.Redis configuration](https://stackexchange.github.io/StackExchange.Redis/Configuration)
