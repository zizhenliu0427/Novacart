# P14 — Modern E-commerce Web App

> Source: UNSW COMP[3/9]900 Capstone Project List (Project #14)
> Client: Dr. Basem Suleiman
> Allowed groups: 2

---

## Project background & goals

This project aims to design and develop a modern, secure, and scalable e-commerce platform for selling products online. The system will provide customers with a seamless purchasing experience while equipping administrators with effective tools to manage products, pricing, inventory, and transactions.

The primary goal is to deliver a **mobile-first, responsive web application with Progressive Web App (PWA) capabilities** that ensures strong security, reliable transaction handling, optimized performance, and an intuitive user experience. The platform should be architected to support future scalability, analytics integration, and feature expansion beyond the MVP scope.

---

## Project scope

Design and implement a secure, scalable, mobile-first e-commerce platform. The system must support common features including:

- Browsing
- Purchasing
- Account management
- Administrative control
- Progressive Web Application (PWA) capabilities for an app-like experience

---

## Project requirements

### User roles

Three user roles with proper access controls:

- **Customer**
- **Administrator**
- **System Administrator**

### Customer capabilities

- Securely register and login, and manage their profile
- Browse / search / filter products and add them to cart and wishlist
- Complete secure checkout and view order history and status

### Admin capabilities

- Manage product details (create, edit, delete) including description, image, price, and other information
- Manage stock levels and availability of different products
- View and update order status and view dashboard with sales analytics
- Configure pricing following different models and rules, and manually
- View history of purchases with the dynamic pricing

### Product Catalogue

- Display product catalogue with specific details based on the product type (products to be labelled with matching details)
- Product keyword search with relevant category filters and sorting

### Shopping Cart & Wishlist

- Customers can add / remove / update quantity while shopping
- Price calculation and updates
- Persist cart supporting **logged users and guests** (server-side and local storage, **merge on login**)
- Maintain wishlist to user profile

### Checkout & Payment

- Order summary preview with secure checkout workflow
- Email / message order confirmation
- Shipping information confirmation and delivery status / updates
- Integration with payment gateway with **payment tokenization (no card storage)**

### Orders & Transactions

- Maintain order details at time of purchase:
  - Timestamp
  - Order ID
  - Product details
  - Price at time of purchase
  - Payment status
  - Shipping status
- Customers can view order history
- Admin can update order status: `pending → paid → processing → shipped → completed → cancelled`

### Admin Dashboard

- Product management, pricing configuration, inventory tracking, order management
- Analytics dashboard:
  - Total sales
  - Orders per day
  - Revenue summary
  - Best-selling products

### Non-functional requirements

- Secure, reliable, scalable and fast performing application
- **PWA**: Responsive design with mobile-first design, optimized layouts for mobile, tablet, desktop, fast loading
- Modern PWA features including web app manifest, installable, service worker implementation, standalone app-like mode

---

## Required knowledge and skills

> Note: The original PDF specifies Python FastAPI as backend. Since this is being adapted as a personal project, the stack can be substituted (e.g., ASP.NET Core in place of FastAPI) while preserving the same architectural intent.

| Layer | Original spec | Personal project substitute |
|---|---|---|
| Frontend | Next.js (React) & TypeScript, Tailwind CSS, web app manifest & service worker | Same |
| Backend | Python FastAPI, REST APIs, Celery + Redis (emails, file processing, async tasks) | **ASP.NET Core**, REST APIs, **Hangfire / Background Service + Redis** |
| Data & Storage | PostgreSQL, Redis (preferred), S3-compatible storage (preferred) | Same |
| Cloud | AWS, Docker | Same |

---

## Expected outcomes and deliverables

1. A functional PWA with responsive design
2. Documented source code
3. Comprehensive technical documentation covering:
   - Requirements
   - Architecture and design
   - UI designs
   - Testing APIs
   - Deployment environment with Docker
   - CI/CD
   - Demo materials
4. User guide and instructions

---

## Disciplines related to the project

- Web Application Development
- Cloud Computing
- Human Computer Interaction (HCI)

---

## Resources provided

To be discussed.

---

## Adaptation notes (for personal project)

This document is the canonical reference. Phase planning, GitHub issues, and architectural decisions live in companion files:

- `GitHub_Issues_by_Sprint.md` — Sprint-organized backlog
- `Database_ER_Diagram.md` — Schema with future-proof design decisions
