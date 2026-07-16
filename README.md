# Novacart — Modern E-Commerce Web Application

[![CI](https://github.com/zizhenliu0427/Novacart/actions/workflows/ci.yml/badge.svg)](https://github.com/zizhenliu0427/Novacart/actions/workflows/ci.yml)
> 中文版：[README_CN.md](README_CN.md)

A modern e-commerce platform built with .NET backend and Next.js frontend. **Default deployment is microservices** (YARP gateway + Auth/Product/Cart/Order + RabbitMQ + MassTransit Saga). Legacy monolith: `docker-compose.monolith.yml`.

---

## Table of Contents

- [Overview](#overview)
- [Priority 1 — MVP Core Features](#priority-1--mvp-core-features)
- [Priority 2 — P14 Project Requirements](#priority-2--p14-project-requirements)
- [Priority 3 — Technical Enhancements](#priority-3--technical-enhancements)
- [Planned Enhancements](#planned-enhancements)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [API Reference](#api-reference)
- [Deployment](#deployment)
- [Licence](#licence)

---

## Overview

Novacart is a full-stack e-commerce web application. The MVP delivers five core features: user authentication, product browsing, shopping cart, checkout with Stripe, and order history. Built on a solid foundation with .NET, Next.js, PostgreSQL, Redis, and Docker, the platform is designed to scale.

---

## Priority 1 — MVP Core Features

> These are the **must-have** features. The project is not complete without them.

### 1. User Registration & Login

- Register and login with **JWT (JSON Web Token)** authentication
- Passwords hashed with **bcrypt** (salted, one-way)
- Session management: short-lived access JWT + rotated refresh token in **HttpOnly cookies** (prevents XSS)
- Protected routes — unauthenticated users redirected to login

### 2. Product Browsing

- Product catalogue from **PostgreSQL seed data** (12 products, 5 categories), with **Square Catalogue API** sync in admin (P2)
- Display product name, description, images, and price
- Search and filter products by keyword and category

### 3. Shopping Cart

- Add, remove, update product quantities
- Real-time price calculation
- Cart state managed via **React Context API** + reusable components (CartItem, CartSummary)
- Cart persists across sessions for logged-in users

### 4. Checkout & Payment

- Order summary preview before payment
- **Stripe sandbox** payment processing (test mode, no real charges)
- **Stripe Webhooks** for payment confirmation
- **ngrok** to expose local webhook endpoints during development
- No card details stored on server (tokenisation)

### 5. Order History

- View all previous orders with full details
- Orders stored in **PostgreSQL** (persistent)
- **Redis** caching for fast retrieval of recent orders
- Order details: timestamp, order ID, products, price, payment status

---

## Priority 2 — P14 Project Requirements

> These features come from the UNSW COMP9900 P14 project specification. Implement after MVP is complete.

### 6. PWA & Responsive Design

- **Progressive Web App**: Web App Manifest, Service Worker, installable, standalone mode
- **Mobile-first responsive layout**:
  - CSS Grid with adaptive breakpoints: 4 → 3 → 2 → 1 columns
  - rem units for font sizing and spacing (no hardcoded pixels)
  - CSS custom properties for consistent spacing
  - Media queries for device-specific adjustments

### 7. Admin Dashboard (P14 Requirement)

- Product management (CRUD)
- Order status management (`pending → paid → processing → shipped → completed → cancelled`)
- Sales analytics dashboard (ECharts): total sales, orders per day, revenue summary

### 8. Role-Based Access Control (P14 Requirement)

- Three roles: **Customer**, **Administrator**, **System Administrator**
- Role claims embedded in JWT token
- Admin-only endpoints reject non-admin tokens (403 Forbidden)

### 9. Advanced Search & Filtering (P14 Requirement)

- Multi-category search with type-based filtering and sorting
- Product keyword search

---

## Priority 3 — Technical Enhancements

> These demonstrate engineering best practices and technical depth. Add as time permits.

### 10. Frontend Architecture

- **Reusable components**: Sidebar, Header, ProductCard, CartItem (~40% code reduction)
- **Custom React Hooks**: `useAuth`, `useCart` (via Context providers)
- **Responsive Sidebar**: auto-collapse at ≤768px breakpoint
- **Nested routing with route guards**: Next.js App Router + Middleware

### 11. API & Documentation

- **Unified API layer**: `apiCall` wrapper with cookie auth (`credentials: 'include'`), automatic refresh on 401, error parsing, 401/403 handling
- **Swagger / OpenAPI**: auto-generated interactive API docs (Swashbuckle)

### 12. Testing & CI/CD

- **Backend**: xUnit unit & integration tests
- **Frontend**: Vitest + React Testing Library
- **CI/CD**: GitHub Actions (auto-build, auto-test, deploy)

### 13. Code Quality

- **Layered architecture**: Controller → Service → Mapper → Entity
- **Design patterns**: Factory Pattern (object creation), Strategy Pattern (payment providers)
- **Database standards**: Alibaba Development Standards (naming, indexing, SQL guidelines)

---

## P14 Preferred Deliverables (Completed)

> The P14 spec lists several "preferred" capabilities beyond the core requirements. These are now implemented:

| Capability | Implementation |
|---|---|
| **JWT Refresh Tokens** | Short-lived access tokens (15 min) + rotated, DB-persisted refresh tokens (7 days) in HttpOnly cookies with reuse detection. See [Architecture §4](docs/ARCHITECTURE.md#4-cross-cutting-infrastructure). |
| **Async Email Queue** | MailKit SMTP behind a bounded `Channel<T>` + `BackgroundService` worker, so webhook/request handlers enqueue and return immediately. See [Architecture §5](docs/ARCHITECTURE.md#5-data-flow--checkout--payment-end-to-end). |
| **S3 Object Storage** | `IS3StorageService` with presigned PUT/GET URLs; admin uploads product images directly to S3. Backed by **LocalStack** in dev (no AWS account needed) and real AWS in production via configuration. See [Deployment Guide](docs/deployment-guide.md). |

### P14 Documentation Deliverables

The P14 spec requires comprehensive technical documentation. All deliverables now exist:

| Deliverable | Document |
|---|---|
| Architecture & design | [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) |
| UI design | [docs/UI-DESIGN.md](docs/UI-DESIGN.md) |
| Database schema | [Database_ER_Diagram.md](Database_ER_Diagram.md) + [docs/database-standards.md](docs/database-standards.md) |
| Testing APIs | [Swagger](http://localhost:5000/swagger) (auto-generated) + containerized test suites ([Dockerfile.backend.test](Dockerfile.backend.test), [Dockerfile.frontend.test](Dockerfile.frontend.test)) |
| Deployment | [docs/deployment-guide.md](docs/deployment-guide.md) |
| CI/CD | [.github/workflows/ci.yml](.github/workflows/ci.yml) |
| User guide | [docs/USER-GUIDE.md](docs/USER-GUIDE.md) |
| Demo materials | [docs/DEMO.md](docs/DEMO.md) |
| Technical notes | [TECH_NOTES.md](TECH_NOTES.md) |

---

## Planned Enhancements

> Scaling enhancements PE-1 through PE-7 and PE-10 are **implemented** (PE-6/PE-7 off by default). Remaining: PE-8, PE-9. Tracked in **[TODO.md](TODO.md)** and [HANDOFF.md §11](HANDOFF.md#11-planned-enhancements-scaling-tail--not-scheduled).

| Technology | Purpose |
|---|---|
| **Microservices (PE-1 final)** | ✅ **.NET Aspire** + **YARP** + **Polly**; Auth / Product / Cart / Order. **MassTransit + RabbitMQ**, **Outbox**, **Saga**. [docs/MICROSERVICES-PE1.md](docs/MICROSERVICES-PE1.md) |
| **RabbitMQ + async orders** | ✅ PE-1 + PE-5 admin saga/DLQ UI |
| **ElasticSearch** | ✅ **PE-3** — Product API keyword search; Postgres fallback |
| **Distributed Lock & inventory (PE-4)** | ✅ **Complete** — Redlock + checkout holds (TTL) + atomic SQL + YARP rate limit + Redis HA docs + OTel metrics. [docs/PE4-PRODUCTION-HARDENING.md](docs/PE4-PRODUCTION-HARDENING.md) |
| **Async order processing (PE-5)** | ✅ MassTransit Saga + admin retry UI |
| **Cart Optimisation (PE-6)** | ✅ Redis cart snapshot; Postgres source of truth; `CartRedis.Enabled=false` default. [docs/PE6-REDIS-CART.md](docs/PE6-REDIS-CART.md) |
| **SQL Sharding (PE-7)** | ✅ UserId-hash order pilot; `OrderSharding.Enabled=false` default. [docs/PE7-SQL-SHARDING.md](docs/PE7-SQL-SHARDING.md) |
| **Thread Pool Tuning (PE-8)** | Custom thread pool for flash sales — not started |
| **AI Chatbot (PE-9)** | OpenAI / Ollama support bot — not started |
| **Internationalisation (PE-10)** | ✅ `/en/` + `/zh/` via next-intl |

### Spring Cloud large mall — inventory & checkout (reference)

Production Java/Spring Cloud shopping platforms typically combine **several** layers (not Redis lock alone):

| Layer | Typical Spring stack | Novacart (.NET) |
|---|---|---|
| Gateway + rate limit | Spring Cloud Gateway + Sentinel | ✅ YARP fixed-window limiter (PE-4) |
| Checkout orchestration | Spring Cloud Stream + Saga / Seata | ✅ MassTransit Saga + Outbox |
| Stock deduct lock | Redisson / Redis | ✅ `RedisDistributedLockService` |
| Stock **reservation** | Redis/DB hold + TTL | ✅ `StockHoldService` + TTL worker (PE-4) |
| DB last defence | `UPDATE … WHERE stock >= qty` | ✅ `ProductStockRepository` conditional UPDATE (PE-4) |
| Flash-sale queue | MQ + 限流 | ✅ YARP rate limit on checkout (PE-4) |
| Cart cache | Redis | ✅ Redis snapshot + Postgres truth (PE-6, off by default) |
| Order sharding | ShardingSphere / custom routing | ✅ UserId-hash pilot (PE-7, off by default) |
| Search | Elasticsearch | ✅ PE-3 |

Details: [PE4-PRODUCTION-HARDENING.md](docs/PE4-PRODUCTION-HARDENING.md) · [PE6-REDIS-CART.md](docs/PE6-REDIS-CART.md) · [PE7-SQL-SHARDING.md](docs/PE7-SQL-SHARDING.md) · [MICROSERVICES-PE1.md § Spring Cloud](docs/MICROSERVICES-PE1.md#4-spring-cloud-comparison-final-novacart-row)

---

## Tech Stack

### MVP Stack (Priority 1)

| Layer | Technology |
|---|---|
| **Frontend** | Next.js 14+ (React), TypeScript, Tailwind CSS |
| **Backend** | ASP.NET Core 8+ (C#), RESTful APIs |
| **ORM** | Entity Framework Core (EF Core) |
| **Database** | PostgreSQL 16+ |
| **Cache** | Redis 7+ |
| **Payment** | Stripe API (sandbox) |
| **Product Data** | PostgreSQL seed data (P1); Square Catalogue API (P2) |

| **Auth** | JWT (HS256) + bcrypt |
| **Webhook** | ngrok |
| **Container** | Docker, Docker Compose |
| **Cloud** | AWS (EC2, RDS, ElastiCache, S3) |

### Extended Stack (Priority 2 & 3)

| Layer | Technology |
|---|---|
| **PWA** | Web App Manifest, Service Worker |
| **CSS** | CSS Grid, rem, custom properties, media queries |
| **State Mgmt** | React Context API (`AuthContext`, `CartContext`, `WishlistContext`, `ToastContext`) |
| **Components** | Reusable library + Custom Hooks |
| **Charts** | ECharts (admin dashboard) |
| **API Docs** | Swagger / OpenAPI (Swashbuckle) |
| **Testing (BE)** | xUnit |
| **Testing (FE)** | Vitest + React Testing Library |
| **Architecture** | Controller → Service → Mapper → Entity |
| **Design** | Factory Pattern, Strategy Pattern |
| **DB Standards** | Alibaba Development Standards |
| **CI/CD** | GitHub Actions |

### Why This Stack?

**MVP decisions:**
- **ASP.NET Core** — High-performance, cross-platform backend with built-in DI
- **EF Core** — Official .NET ORM, Code-First migrations, LINQ queries
- **Next.js** — SSR, file-based routing, optimised React development
- **PostgreSQL** — Robust relational DB with strong query and transaction support
- **Redis** — In-memory caching for sessions, cart, and recent orders
- **Stripe** — Industry-standard payment with excellent sandbox mode
- **Docker** — Consistent dev and deployment environments

**Engineering practices:**
- **Layered architecture** — Clean separation: Controllers (HTTP), Services (logic), Mappers (data), Entities (models)
- **Factory & Strategy patterns** — Extensible payment providers, clean object creation
- **Alibaba Standards** — Consistent DB conventions: `idx_table_column` naming, `BIGINT` for IDs, `DECIMAL` for money

---

## Architecture

```
┌─────────────┐     HTTPS      ┌─────────────────┐     REST API     ┌──────────────────┐
│   Browser   │ ──────────────>│    Next.js       │ ───────────────>│  ASP.NET Core     │
│  (Client)   │ <──────────────│   Frontend       │ <───────────────│  Backend API      │
└─────────────┘                └─────────────────┘                  └────────┬─────────┘
                                                                            │
                                                          ┌─────────────────┼─────────────────┐
                                                          │                 │                 │
                                                          ▼                 ▼                 ▼
                                                   ┌────────────┐   ┌────────────┐   ┌────────────┐
                                                   │ PostgreSQL │   │    Redis   │   │   Stripe   │
                                                   │  (Orders,  │   │  (Cache,   │   │  (Payment  │
                                                   │   Users)   │   │  Sessions) │   │  Webhook)  │
                                                   └────────────┘   └────────────┘   └────────────┘
                                                                                          ▲
                                                                                          │
                                                                                     ┌────┴────┐
                                                                                     │  ngrok  │
                                                                                     │ (Tunnel)│
                                                                                     └─────────┘
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker & Docker Compose](https://www.docker.com/get-started)
- [PostgreSQL 16+](https://www.postgresql.org/download/) (or via Docker)
- [Redis 7+](https://redis.io/download/) (or via Docker)
- [Stripe CLI](https://stripe.com/docs/stripe-cli) (for webhook testing)
- [ngrok](https://ngrok.com/) (for exposing local webhook endpoints)

### Quick Start with Docker

```bash
# Clone the repository
git clone https://github.com/your-username/novacart.git
cd novacart

# Copy environment variables
cp .env.example .env
# Edit .env with your Stripe keys, database credentials, etc.

# Start all services (build + run in background)
docker compose up --build -d

# Verify microservices smoke (gateway health + products)
chmod +x scripts/e2e-microservices-smoke.sh
./scripts/e2e-microservices-smoke.sh

# Stop all services
docker compose down

# Stop and delete all data (database, cache)
docker compose down -v
```

### Service URLs

| Service | URL | Description |
|---|---|---|
| **Frontend** | http://localhost:3000 | Next.js application |
| **YARP Gateway** | http://localhost:5000 | API entry (`/api/*`) |
| **Health Check** | http://localhost:5000/api/health | Order service health (via gateway) |
| **Jaeger UI** | http://localhost:16686 | OpenTelemetry traces |
| **RabbitMQ** | http://localhost:15672 | Management UI (guest/guest) |
| **PostgreSQL** | localhost:5432 | 4 logical DBs (see [DATABASE-PER-SERVICE.md](docs/DATABASE-PER-SERVICE.md)) |
| **Redis** | localhost:6379 | Cache |

Legacy monolith: `docker compose -f docker-compose.monolith.yml up --build -d` (Swagger at `:5000/swagger`).

### Docker Commands

```bash
# Start all services
docker compose up --build -d

# Check service status
docker compose ps

# View logs (all services)
docker compose logs -f

# View logs (specific service)
docker compose logs -f order-api
docker compose logs -f gateway
docker compose logs -f frontend

# Restart a specific service
docker compose restart backend

# Rebuild a specific service
docker compose up --build -d backend

# Stop all services
docker compose down

# Stop and delete all data volumes
docker compose down -v

# Enter a running container
docker exec -it novacart-backend-1 bash
docker exec -it novacart-postgres-1 psql -U postgres -d novacart
```

### Manual Setup

#### Backend (ASP.NET Core)

```bash
cd backend
dotnet restore
dotnet ef database update
dotnet run --launch-profile "Development"
```

#### Frontend (Next.js)

```bash
cd frontend
npm install
npm run dev
```

#### Stripe Webhook (with ngrok)

```bash
# Terminal 1: Start ngrok tunnel
ngrok http 5000

# Terminal 2: Forward Stripe events
stripe listen --forward-to localhost:5000/api/webhooks/stripe
```

### Environment Variables

Create a `.env` file in the project root:

```env
# Database
DATABASE_URL=postgresql://postgres:password@localhost:5432/novacart

# Redis
REDIS_URL=localhost:6379

# JWT
JWT_SECRET=your-super-secret-key-change-in-production
JWT_EXPIRY_HOURS=24

# Stripe (sandbox)
STRIPE_SECRET_KEY=sk_test_...
STRIPE_PUBLISHABLE_KEY=pk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...

# Square (sandbox — P2)
SQUARE_ACCESS_TOKEN=EAAAl...
SQUARE_ENVIRONMENT=sandbox

# ngrok
NGROK_URL=https://your-ngrok-url.ngrok-free.app
```

---

## Project Structure

```
novacart/
├── frontend/                    # Next.js application
│   ├── src/
│   │   ├── app/                 # App router pages
│   │   │   ├── (auth)/          # Login & register
│   │   │   ├── products/        # Product browsing
│   │   │   ├── cart/            # Shopping cart
│   │   │   ├── checkout/        # Checkout flow
│   │   │   └── orders/          # Order history
│   │   ├── components/          # Reusable UI components
│   │   ├── hooks/               # Custom React hooks
│   │   ├── lib/                 # Utility functions & API clients
│   │   ├── store/               # State management (cart, auth)
│   │   └── types/               # TypeScript type definitions
│   ├── public/
│   ├── tailwind.config.ts
│   ├── next.config.ts
│   └── package.json
│
├── backend/                     # ASP.NET Core Web API
│   ├── Controllers/             # API controllers
│   ├── Models/                  # Domain models & DTOs
│   ├── Services/                # Business logic layer
│   ├── Data/                    # DbContext & migrations
│   ├── Middleware/              # JWT auth, error handling
│   └── Program.cs               # Application entry point
│
├── docker-compose.yml
├── Dockerfile.frontend
├── Dockerfile.backend
├── .env.example
└── README.md
```

---

## API Reference

### Authentication

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/auth/register` | Register a new user |
| POST | `/api/auth/login` | Login and receive JWT + refresh token (HttpOnly cookies) |
| POST | `/api/auth/refresh` | Rotate access + refresh tokens |
| POST | `/api/auth/logout` | Revoke refresh tokens and clear cookies |

### Products

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/products` | List all products (paginated) |
| GET | `/api/products/{id}` | Get product details |
| GET | `/api/products/search` | Search products by keyword |

### Cart

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/cart` | Get current user's cart |
| POST | `/api/cart/items` | Add item to cart |
| PUT | `/api/cart/items/{id}` | Update item quantity |
| DELETE | `/api/cart/items/{id}` | Remove item from cart |

### Checkout & Payments

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/checkout` | Create checkout session |
| POST | `/api/webhooks/stripe` | Stripe webhook endpoint |

### Orders

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/orders` | Get user's order history |
| GET | `/api/orders/{id}` | Get order details |

---

## Deployment

### AWS Architecture

```
                    ┌─────────────────────────────────────────┐
                    │              AWS Cloud                   │
                    │                                         │
                    │   ┌──────────┐    ┌──────────────────┐  │
  Users ───────────>│   │  EC2     │    │   RDS            │  │
  (HTTPS)           │   │  (App)   │───>│   (PostgreSQL)   │  │
                    │   └──────────┘    └──────────────────┘  │
                    │        │                                 │
                    │        │          ┌──────────────────┐  │
                    │        └─────────>│   ElastiCache    │  │
                    │                   │   (Redis)        │  │
                    │                   └──────────────────┘  │
                    │                                         │
                    │                   ┌──────────────────┐  │
                    │                   │   S3             │  │
                    │                   │   (Static Assets)│  │
                    │                   └──────────────────┘  │
                    └─────────────────────────────────────────┘
```

### Docker Compose (Production)

```bash
docker compose -f docker-compose.prod.yml up -d
```

---

## Licence

This project is licensed under the MIT Licence. See the [LICENCE](LICENCE) file for details.

---

## Acknowledgements

- [Stripe Documentation](https://stripe.com/docs)
- [Square Developer Docs](https://developer.squareup.com/docs)
- [Next.js Documentation](https://nextjs.org/docs)
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [UNSW COMP3900/9900 Capstone Project](https://www.cse.unsw.edu.au/~cs3900/) — Project #14
