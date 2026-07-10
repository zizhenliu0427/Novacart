# Novacart — Modern E-Commerce Web Application

> 中文版：[README_CN.md](README_CN.md)

A modern e-commerce platform built with .NET backend and Next.js frontend. This project follows a **MonolithFirst** approach — a simple, functional MVP as the foundation, with scalability and advanced features planned for future iterations.

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
- Session management: JWT stored in **HttpOnly cookies** (prevents XSS)
- Protected routes — unauthenticated users redirected to login

### 2. Product Browsing

- Product catalogue from **PostgreSQL seed data** (12 products, 5 categories) — **Square Catalogue API** integration planned for P2
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
- **Custom React Hooks**: `useAuth`, `useCart`, `useApi` (preferred over HOC)
- **Responsive Sidebar**: auto-collapse at ≤768px breakpoint
- **Nested routing with route guards**: Next.js App Router + Middleware

### 11. API & Documentation

- **Unified API layer**: `apiCall` wrapper with auto token injection, error parsing, 401/403 handling
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

## Planned Enhancements

> Future iterations as the platform scales. Not part of the current MVP.

| Technology | Purpose |
|---|---|
| **Microservice Architecture** | Decompose monolith into independent services (Auth, Product, Cart, Order). Consul for service discovery, YARP/Ocelot as API gateway, Polly for circuit breaking. |
| **RabbitMQ** | Async order processing, inventory updates, email notifications. |
| **ElasticSearch** | Full-text search for product catalogues (material, style, price). |
| **Distributed Lock (Redis)** | Redlock for atomic inventory deduction across multiple instances. |
| **Async Order Processing** | Decouple checkout workflows (payment → inventory → email) via RabbitMQ. |
| **Cart Optimisation** | Redis-backed cart: sub-ms reads, cross-device sync, guest-to-user merge. |
| **SQL Sharding** | Horizontal partitioning of large tables by date or user ID. |
| **Thread Pool Tuning** | Custom thread pool for flash sales and bulk order processing. |
| **AI Chatbot (Low Priority)** | Customer service bot via OpenAI API or Ollama (local LLM). |
| **Internationalisation (i18n)** | Bilingual UI (Chinese/English) with URL-based language routing. |

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
| **State Mgmt** | React Context API / Zustand |
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

# Stop all services
docker compose down

# Stop and delete all data (database, cache)
docker compose down -v
```

### Service URLs

| Service | URL | Description |
|---|---|---|
| **Frontend** | http://localhost:3000 | Next.js application |
| **Backend API** | http://localhost:5000 | ASP.NET Core Web API |
| **Swagger Docs** | http://localhost:5000/swagger | API documentation |
| **Health Check** | http://localhost:5000/api/health | Backend health status |
| **PostgreSQL** | localhost:5432 | Database (user: postgres, pass: postgres) |
| **Redis** | localhost:6379 | Cache |

### Docker Commands

```bash
# Start all services
docker compose up --build -d

# Check service status
docker compose ps

# View logs (all services)
docker compose logs -f

# View logs (specific service)
docker compose logs -f backend
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
| POST | `/api/auth/login` | Login and receive JWT |
| POST | `/api/auth/logout` | Invalidate session |

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
