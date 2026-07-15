# Novacart Deployment Guide

> P3-6 deliverable: deployment documentation for Docker and AWS.

---

## Quick Start — Local Production Mode

```bash
# 1. Copy env template and fill in values
cp .env.example .env
# Edit .env with real DB, Redis, JWT, Stripe, SMTP credentials

# 2. Start with production compose overlay
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build

# 3. Verify health
curl http://localhost:5000/api/health
# Expected: {"status":"healthy","database":true,"redis":true,...}
```

> **Note:** `docker-compose.prod.yml` is an *override* file. It disables the embedded
> Postgres/Redis containers (moved to `dev-only` profile) and injects secrets from `.env`.

---

## Development Environment

```bash
# Full dev stack with embedded Postgres + Redis
docker compose up -d --build

# Or run services locally:
# Backend: cd backend && dotnet run
# Frontend: cd frontend && npm run dev
```

Default dev credentials (from `appsettings.Development.json`):
- Admin: `admin@novacart.local` / `Admin123!`
- Postgres: `postgres:postgres` on port 5432
- Redis: port 6379

---

## AWS Deployment Architecture

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│  CloudFront  │───▶│   EC2 / ECS  │───▶│   RDS        │
│  (CDN+HTTPS) │    │  Backend     │    │  PostgreSQL  │
└─────────────┘    │  Frontend    │    │  16          │
                   └──────┬──────┘    └─────────────┘
                          │
                   ┌──────▼──────┐
                   │ ElastiCache  │
                   │ Redis 7      │
                   └─────────────┘
```

### AWS Resources

| Service | Purpose | Configuration |
|---|---|---|
| **EC2** or **ECS Fargate** | Run backend + frontend containers | t3.small minimum, Docker Compose or ECS task definition |
| **RDS PostgreSQL 16** | Primary database | db.t3.micro, Multi-AZ for production |
| **ElastiCache Redis 7** | Caching layer | cache.t3.micro, single-node for MVP |
| **S3** | Product images, static assets | Public read bucket with CloudFront CDN |
| **ACM** | TLS certificates | Free, auto-renewed |
| **CloudFront** | CDN + HTTPS termination | Origin: EC2/ALB |

### Setup Steps

1. **RDS**: Create PostgreSQL 16 instance, note the endpoint
2. **ElastiCache**: Create Redis 7 cluster, note the endpoint
3. **EC2**: Launch instance with Docker installed
4. **Deploy**:
   ```bash
   # On EC2 instance
   git clone <repo>
   cp .env.example .env
   # Fill in RDS endpoint, ElastiCache endpoint, secrets
   docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
   ```
5. **HTTPS**: Set up ACM certificate + CloudFront or ALB with TLS termination

---

## Environment Variables Reference

See [`.env.example`](../.env.example) for the complete list. Critical variables:

| Variable | Required | Description |
|---|---|---|
| `DB_CONNECTION_STRING` | ✅ | PostgreSQL connection string |
| `REDIS_CONNECTION_STRING` | ✅ | Redis host:port |
| `JWT_SECRET` | ✅ | ≥32 char secret for JWT signing |
| `STRIPE_SECRET_KEY` | ✅ | Stripe API secret key |
| `STRIPE_WEBHOOK_SECRET` | ✅ | Stripe webhook endpoint secret |
| `SMTP_HOST` | ✅ | SMTP server for order emails |
| `SMTP_USERNAME` / `SMTP_PASSWORD` | ✅ | SMTP credentials |
| `SQUARE_ACCESS_TOKEN` | Optional | Square API for catalogue sync |
| `Aws__S3__Bucket` | Dev / Prod | S3 bucket name for product images |
| `Aws__S3__ServiceUrl` | Dev only | Set to `http://localstack:4566` for LocalStack; **unset in production** to use real AWS |
| `Aws__S3__PublicBaseUrl` | Optional | Stable public URL prefix (e.g. CloudFront origin) |

---

## Local development — S3 / LocalStack

The default `docker-compose.yml` includes **LocalStack** (port `4566`) and runs `localstack/init-s3-bucket.sh` on startup to create the `novacart-product-images` bucket. No AWS account is needed for local admin image uploads.

Backend configuration (also in `backend/appsettings.json`):

```env
Aws__S3__Bucket=novacart-product-images
Aws__S3__ServiceUrl=http://localstack:4566
Aws__S3__ForcePathStyle=true
Aws__S3__PublicBaseUrl=http://localhost:4566/novacart-product-images
```

**Production:** remove or leave `Aws__S3__ServiceUrl` empty so `S3StorageService` uses the AWS SDK default credential chain (IAM role / env keys) against real S3.

---

## Health Check

The `/api/health` endpoint probes both database and Redis connectivity:

```json
// Healthy response (200 OK)
{
  "status": "healthy",
  "timestamp": "2026-07-11T06:00:00Z",
  "environment": "Production",
  "database": true,
  "redis": true
}

// Degraded response (503 Service Unavailable)
{
  "status": "degraded",
  "database": false,
  "redis": true
}
```

Use this endpoint for container healthchecks and load balancer target group health.

---

## CI/CD

GitHub Actions CI pipeline (`.github/workflows/ci.yml`) runs on every push/PR:

1. **backend-test**: .NET restore → build → xUnit tests with coverage
2. **frontend-test**: npm ci → Vitest tests
3. **frontend-build**: Next.js production build (TypeScript check)
4. **docker-build**: Verify Docker images build successfully

Deploy job can be added once the target environment is provisioned.

---

## Security Checklist

- [ ] All secrets in `.env` or AWS Secrets Manager — never in `appsettings.json`
- [ ] `ASPNETCORE_ENVIRONMENT=Production` (set by `docker-compose.prod.yml`)
- [ ] HTTPS enforced via CloudFront/ALB
- [ ] CORS restricted to production domain (update `AllowFrontend` policy)
- [ ] Stripe webhook signature verification enabled
- [ ] JWT secret ≥32 characters, rotated periodically
- [ ] Database credentials use a dedicated app user (not `postgres`)
