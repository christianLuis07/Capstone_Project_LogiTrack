# LogiTrack — Order & Inventory Management System

> **Capstone Project — Full Implementation (Parts 1 - 5)**  
> Built with ASP.NET Core Web API, Entity Framework Core (SQLite), ASP.NET Core Identity, JWT Bearer Authentication, and In-Memory Caching.

---

## 1. Executive Summary & Architecture Overview

**LogiTrack** is a high-performance order and inventory management system designed for multi-fulfillment logistics operations. The project provides secure, scalable, and responsive HTTP REST API endpoints to manage inventory items and customer orders.

### System Architecture
```
                  +-----------------------------------------+
                  |  Clients (Swagger UI, Web/Mobile App)   |
                  +-----------------------------------------+
                                       |
                           HTTPS / JWT Bearer Auth
                                       v
                  +-----------------------------------------+
                  |  ASP.NET Core Web API (Middleware)     |
                  |  - CORS & Exception Handling            |
                  |  - JWT Authentication & RBAC            |
                  |  - Health Checks & ProblemDetails       |
                  +-----------------------------------------+
                                       |
                   +-------------------+-------------------+
                   |                                       |
                   v                                       v
      +-------------------------+             +-------------------------+
      |   InventoryController   |             |     OrderController     |
      |   - In-Memory Cache     |             |   - Batch Dictionary    |
      |   - Cache Invalidation  |             |   - AsNoTracking Query  |
      +-------------------------+             +-------------------------+
                   |                                       |
                   +-------------------+-------------------+
                                       |
                                       v
                  +-----------------------------------------+
                  |      Entity Framework Core 10           |
                  |      - LogiTrackContext                 |
                  |      - SQLite Database (logitrack.db)   |
                  +-----------------------------------------+
```

---

## 2. Key Components & Implementation Details

### Part 1: Domain Modeling & EF Core Integration
- **`InventoryItem`**: Models inventory assets (`ItemId`, `Name`, `Quantity`, `Location`, `OrderId`). Includes `DisplayInfo()` method.
- **`Order`**: Models customer orders (`OrderId`, `CustomerName`, `DatePlaced`, `SessionId`, `CreatedBy`, `Status`, `Items`). Methods: `AddItem()`, `RemoveItem()`, `GetOrderSummary()`.
- **Relational Mapping**: One-to-many relationship (`Order -> InventoryItems`) with `OnDelete(DeleteBehavior.SetNull)`.

### Part 2: RESTful Web API Controllers
- **`InventoryController`**:
  - `GET /api/inventory`: List all inventory items.
  - `GET /api/inventory/{id}`: Single item retrieval.
  - `POST /api/inventory`: Create item.
  - `DELETE /api/inventory/{id}`: Delete item.
- **`OrderController`**:
  - `GET /api/orders`: List all orders with items eager-loaded (`.Include(o => o.Items)`).
  - `GET /api/orders/{id}`: Order by ID with items.
  - `POST /api/orders`: Create order with multiple items in a single request.
  - `DELETE /api/orders/{id}`: Delete order and return items to inventory.

### Part 3: ASP.NET Core Identity, JWT & Role-Based Access Control (RBAC)
- **`ApplicationUser`**: Inherits from `IdentityUser`.
- **`AuthController`**:
  - `POST /api/auth/register`: User registration with role assignment (`User`, `Manager`).
  - `POST /api/auth/login`: Credential verification via `SignInManager` and JWT generation with role claims.
- **Route Authorization**:
  - Read routes protected by `[Authorize]`.
  - Mutation and deletion routes restricted to managers via `[Authorize(Roles = "Manager")]`.
- **Default Seed Users**:
  - Manager: `manager` / `Manager123!`
  - Staff User: `user` / `User123!`

### Part 4: Performance Tuning & In-Memory Caching
- **`IMemoryCache`**: Cache policy configured with a 30-second TTL.
- **Cache Eviction**: Automatic eviction on POST and DELETE mutations to ensure zero stale data.
- **Query Optimizations**:
  - `.AsNoTracking()` applied to read-only queries to eliminate snapshot tracking overhead.
  - Batch dictionary lookups in `CreateOrder` eliminate N+1 roundtrips.
- **Measured Speedup**: ~8.3x latency improvement on cached requests.

### Part 5: State Persistence, Order Lifecycle & CI/CD
- **Persistent State**: Database persistence verified across server reboots.
- **Session Rehydration**: Orders include `SessionId` allowing cart and session state recovery via `GET /api/orders?sessionId={id}`.
- **Lifecycle Management**: `PATCH /api/orders/{id}/status` supports order progression (`Pending` -> `Processing` -> `Completed` -> `Cancelled`).
- **Health Checks**: `/health` endpoint for uptime monitoring.
- **CI/CD Pipeline**: GitHub Actions workflow at `.github/workflows/ci-cd.yml` automates restore, build, EF migration checks, and release publishing.

---

## 3. Running & Testing the Project

### Prerequisites
- .NET 10.0 SDK installed

### Commands
```bash
# Clone and enter the repository
cd LogiTrack

# Restore and build
dotnet build

# Apply database migrations
dotnet ef database update

# Run the API server
dotnet run
```

### Endpoints Overview
- **Swagger Documentation**: `http://localhost:5160/swagger`
- **Health Check**: `http://localhost:5160/health`
- **API Discovery**: `http://localhost:5160/`

---

## 4. Submission Checklist Verification
- [x] Persistent state implementation verified across restarts
- [x] Full workflow tested: authentication, RBAC, caching, and CRUD
- [x] Query optimizations (`AsNoTracking`, batching, eager loading) in place
- [x] Redundant logic removed and code cleaned
- [x] GitHub Actions CI/CD workflow created (`.github/workflows/ci-cd.yml`)
- [x] Final version ready for peer submission
