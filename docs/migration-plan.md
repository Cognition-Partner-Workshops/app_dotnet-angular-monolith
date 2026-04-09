# OrderManager Monolith-to-Microservices Migration Plan

> **Repository:** `Cognition-Partner-Workshops/app_dotnet-angular-monolith` (branch: `main`)
> **Stack:** .NET 8 / ASP.NET Core / EF Core / SQLite (backend) + Angular 17 (frontend)
> **Target:** Four independent microservices behind an API gateway

---

## Table of Contents

1. [Current Architecture Analysis](#1-current-architecture-analysis)
2. [Target Architecture](#2-target-architecture)
3. [Dependency Graph](#3-dependency-graph)
4. [Execution Classification Summary](#4-execution-classification-summary)
5. [Phase 0 — Foundation](#5-phase-0--foundation)
6. [Phase 1A — Customer Service Extraction](#6-phase-1a--customer-service-extraction)
7. [Phase 1B — Product Catalog Service Extraction](#7-phase-1b--product-catalog-service-extraction)
8. [Phase 2 — Inventory Service Extraction](#8-phase-2--inventory-service-extraction)
9. [Phase 3 — Order Service Extraction + Saga](#9-phase-3--order-service-extraction--saga)
10. [Phase 4A — API Gateway Setup](#10-phase-4a--api-gateway-setup)
11. [Phase 4B — Angular Frontend Migration](#11-phase-4b--angular-frontend-migration)
12. [Phase 5 — Decommission Monolith](#12-phase-5--decommission-monolith)
13. [Cross-Cutting Concerns](#13-cross-cutting-concerns)
14. [Risk Register](#14-risk-register)

---

## 1. Current Architecture Analysis

### 1.1 Shared Database (Single `AppDbContext`)

All five entity sets live in a single `AppDbContext` (`src/OrderManager.Api/Data/AppDbContext.cs`):

```csharp
public DbSet<Customer> Customers => Set<Customer>();
public DbSet<Product> Products => Set<Product>();
public DbSet<Order> Orders => Set<Order>();
public DbSet<OrderItem> OrderItems => Set<OrderItem>();
public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
```

### 1.2 Entity Model Relationships

| Relationship | Type | Defined In | Navigation Properties |
|---|---|---|---|
| `Customer` ← `Order` | One-to-Many | `AppDbContext.cs:38` | `Customer.Orders`, `Order.Customer` |
| `Order` ← `OrderItem` | One-to-Many | `AppDbContext.cs:45` | `Order.Items`, `OrderItem.Order` |
| `Product` ← `OrderItem` | One-to-Many | `AppDbContext.cs:46` | `Product.OrderItems`, `OrderItem.Product` |
| `Product` ↔ `InventoryItem` | One-to-One | `AppDbContext.cs:53` | `Product.Inventory`, `InventoryItem.Product` |

### 1.3 Cross-Domain Coupling in Services

**`OrderService.CreateOrderAsync`** (`src/OrderManager.Api/Services/OrderService.cs:33-69`) is the critical coupling point. In a single transaction it:

1. **Reads `Customers`** — validates customer exists, reads shipping address fields (line 35-41)
2. **Reads `Products`** — gets product name and price for each line item (line 46-47)
3. **Reads & Writes `InventoryItems`** — checks stock availability and decrements quantity (lines 49-55)
4. **Writes `Orders` + `OrderItems`** — creates order with computed total (lines 57-68)

**`CustomerService.GetCustomerByIdAsync`** (`CustomerService.cs:23`) uses `.Include(c => c.Orders)` — crosses into Order domain.

**`ProductService.GetAllProductsAsync`** / `GetProductByIdAsync` / `GetProductsByCategoryAsync` (`ProductService.cs:18,23,35`) uses `.Include(p => p.Inventory)` — crosses into Inventory domain.

**`InventoryService`** methods (`InventoryService.cs:18,23`) use `.Include(i => i.Product)` — crosses into Product domain.

### 1.4 Service Registration (Flat DI)

All four services are registered as scoped dependencies in `Program.cs:10-13` with no module boundaries:

```csharp
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<InventoryService>();
```

### 1.5 API Surface

| Controller | Route | Endpoints | File |
|---|---|---|---|
| `CustomersController` | `/api/customers` | `GET /`, `GET /{id}`, `POST /` | `Controllers/CustomersController.cs` |
| `ProductsController` | `/api/products` | `GET /`, `GET /{id}`, `GET /category/{category}`, `POST /` | `Controllers/ProductsController.cs` |
| `InventoryController` | `/api/inventory` | `GET /`, `GET /product/{productId}`, `POST /product/{productId}/restock`, `GET /low-stock` | `Controllers/InventoryController.cs` |
| `OrdersController` | `/api/orders` | `GET /`, `GET /{id}`, `POST /`, `PATCH /{id}/status` | `Controllers/OrdersController.cs` |

### 1.6 Angular Frontend API Calls

Each Angular component (`client-app/src/app/modules/`) makes direct `HttpClient` calls to `/api/*`:

| Component | API Call | File |
|---|---|---|
| `OrderListComponent` | `GET /api/orders` | `modules/orders/order-list.component.ts:25` |
| `CustomerListComponent` | `GET /api/customers` | `modules/customers/customer-list.component.ts:24` |
| `ProductListComponent` | `GET /api/products` | `modules/products/product-list.component.ts:24` |
| `InventoryListComponent` | `GET /api/inventory` | `modules/inventory/inventory-list.component.ts:24` |

### 1.7 Seed Data

`SeedData.cs` initializes 3 customers, 5 products, and 5 inventory items. Each extracted service must carry forward its relevant seed data.

---

## 2. Target Architecture

```
                    ┌──────────────────────────────────┐
                    │         Angular Frontend          │
                    │   (client-app/ — unchanged)       │
                    └────────────┬─────────────────────┘
                                 │ /api/*
                    ┌────────────▼─────────────────────┐
                    │         API Gateway               │
                    │  (YARP / Ocelot / nginx)          │
                    └──┬──────┬──────┬──────┬──────────┘
                       │      │      │      │
          ┌────────────▼┐ ┌───▼────┐ ┌▼─────┐ ┌▼──────────┐
          │  Customer   │ │Product │ │Inven- │ │  Order    │
          │  Service    │ │Catalog │ │tory   │ │  Service  │
          │             │ │Service │ │Service│ │  + Saga   │
          └──────┬──────┘ └───┬────┘ └┬──────┘ └┬──────────┘
                 │            │       │         │
          ┌──────▼──────┐ ┌──▼────┐ ┌▼──────┐ ┌▼──────────┐
          │ customer.db │ │prod.db│ │inv.db │ │ order.db  │
          └─────────────┘ └───────┘ └───────┘ └───────────┘
                       │      │      │      │
                    ┌──▼──────▼──────▼──────▼──────────┐
                    │      Message Broker               │
                    │  (RabbitMQ / Azure Service Bus)   │
                    └──────────────────────────────────┘
```

### Target Microservices

| Service | Owned Entities | Database | Port (Dev) |
|---|---|---|---|
| Customer Service | `Customer` | `customer.db` | 5101 |
| Product Catalog Service | `Product` | `product.db` | 5102 |
| Inventory Service | `InventoryItem` | `inventory.db` | 5103 |
| Order Service | `Order`, `OrderItem` | `order.db` | 5104 |
| API Gateway | — | — | 5100 |

---

## 3. Dependency Graph

```
Phase 0 (Foundation) ─── Sequential prerequisite for everything
    │
    ├──► Phase 1A (Customer Service) ──┐
    │         [PARALLEL]               ├──► Phase 3 (Order Service + Saga)
    ├──► Phase 1B (Product Catalog) ───┤         │
    │         │                        │         │
    │         └──► Phase 2 (Inventory) ┘         │
    │              [Sequential after 1B]         │
    │                                            ▼
    │                                    Phase 4A (Gateway) ──┐
    │                                    Phase 4B (Frontend)──┤ [PARALLEL]
    │                                                         │
    │                                                         ▼
    └────────────────────────────────────────────────── Phase 5 (Decommission)
```

### Phase Dependency Details

- **Phase 0** has no dependencies. Must complete before any other phase starts.
- **Phase 1A** depends on Phase 0 only.
- **Phase 1B** depends on Phase 0 only.
- **Phase 1A and 1B run in PARALLEL** — they share no write dependencies.
- **Phase 2** depends on **Phase 1B** — Inventory has a FK relationship to Product (`InventoryItem.ProductId`). The Inventory Service needs the Product Catalog Service API available to maintain referential integrity.
- **Phase 3** depends on **Phases 1A, 1B, and 2** — `OrderService.CreateOrderAsync` calls across all three domains. The saga orchestrator needs all upstream service APIs available.
- **Phase 4A and 4B run in PARALLEL** — gateway routing and frontend migration are independent tasks that both depend on Phase 3.
- **Phase 5** depends on **Phases 4A and 4B** — decommissioning requires all services and the frontend to be fully migrated.

---

## 4. Execution Classification Summary

| Work Unit | Type | Dependencies | Can Parallelize With | Estimated Effort |
|---|---|---|---|---|
| Phase 0: Foundation | Sequential | None | Nothing | Medium |
| Phase 1A: Customer Service | Parallel | Phase 0 | Phase 1B | Small |
| Phase 1B: Product Catalog | Parallel | Phase 0 | Phase 1A | Small |
| Phase 2: Inventory Service | Sequential | Phase 1B | Nothing (waits for 1B) | Medium |
| Phase 3: Order Service + Saga | Sequential | Phases 1A, 1B, 2 | Nothing | Large |
| Phase 4A: API Gateway | Parallel | Phase 3 | Phase 4B | Medium |
| Phase 4B: Frontend Migration | Parallel | Phase 3 | Phase 4A | Small |
| Phase 5: Decommission Monolith | Sequential | Phases 4A, 4B | Nothing | Medium |

---

## 5. Phase 0 — Foundation

> **Type:** Sequential — must complete before all other phases
> **Child Session:** `Phase-0-Foundation`
> **Dependencies:** None

### 5.1 Inputs

- Monolith repository at `Cognition-Partner-Workshops/app_dotnet-angular-monolith` (branch: `main`)
- Current project structure: single `.sln` with one API project and one Angular client app

### 5.2 Tasks

#### 5.2.1 Solution Restructuring

Create a new solution structure to support multiple services:

```
/
├── src/
│   ├── OrderManager.Api/              # Existing monolith (kept running during migration)
│   ├── Services/
│   │   ├── CustomerService.Api/       # New .NET 8 Web API project
│   │   ├── ProductCatalog.Api/        # New .NET 8 Web API project
│   │   ├── InventoryService.Api/      # New .NET 8 Web API project
│   │   └── OrderService.Api/          # New .NET 8 Web API project
│   ├── Gateway/
│   │   └── ApiGateway/                # YARP-based reverse proxy
│   └── Shared/
│       ├── OrderManager.Contracts/    # Shared DTOs, event schemas, API contracts
│       └── OrderManager.Common/       # Cross-cutting: logging, health checks, correlation IDs
├── client-app/                        # Angular frontend (unchanged)
├── docker-compose.yml                 # Local orchestration
├── docs/
│   └── migration-plan.md             # This document
└── OrderManager.sln                   # Updated solution file
```

#### 5.2.2 Shared Contracts NuGet Package (`OrderManager.Contracts`)

Create `src/Shared/OrderManager.Contracts/` containing:

**REST API Contract DTOs:**

```csharp
// Customer contracts
public record CustomerDto(int Id, string Name, string Email, string Phone,
    string Address, string City, string State, string ZipCode, DateTime CreatedAt);
public record CreateCustomerRequest(string Name, string Email, string Phone,
    string Address, string City, string State, string ZipCode);

// Product contracts
public record ProductDto(int Id, string Name, string Description, string Category,
    decimal Price, string Sku, DateTime CreatedAt);
public record CreateProductRequest(string Name, string Description, string Category,
    decimal Price, string Sku);

// Inventory contracts
public record InventoryItemDto(int Id, int ProductId, int QuantityOnHand,
    int ReorderLevel, string WarehouseLocation, DateTime LastRestocked);
public record RestockRequest(int Quantity);
public record ReserveStockRequest(int Quantity);
public record ReserveStockResponse(bool Success, int QuantityReserved, int RemainingStock);

// Order contracts
public record OrderDto(int Id, int CustomerId, DateTime OrderDate, string Status,
    decimal TotalAmount, string ShippingAddress, List<OrderItemDto> Items);
public record OrderItemDto(int Id, int ProductId, int Quantity, decimal UnitPrice);
public record CreateOrderRequest(int CustomerId, List<OrderItemLineRequest> Items);
public record OrderItemLineRequest(int ProductId, int Quantity);
public record UpdateStatusRequest(string Status);
```

**Async Event Schemas:**

```csharp
// Events published by Product Catalog Service
public record ProductCreatedEvent(int ProductId, string Name, string Sku, decimal Price);
public record ProductDeletedEvent(int ProductId);
public record ProductUpdatedEvent(int ProductId, string Name, decimal Price);

// Events published by Order Service
public record OrderPlacedEvent(int OrderId, int CustomerId, List<OrderLineEvent> Items);
public record OrderLineEvent(int ProductId, int Quantity, decimal UnitPrice);
public record OrderCancelledEvent(int OrderId, string Reason);

// Events published by Inventory Service
public record StockReservedEvent(int OrderId, List<ReservedLineEvent> ReservedItems);
public record ReservedLineEvent(int ProductId, int QuantityReserved);
public record StockInsufficientEvent(int OrderId, int ProductId, int RequestedQuantity, int AvailableQuantity);

// Events published by Customer Service
public record CustomerCreatedEvent(int CustomerId, string Name, string Email);
public record CustomerDeletedEvent(int CustomerId);
```

#### 5.2.3 Shared Common Library (`OrderManager.Common`)

Create `src/Shared/OrderManager.Common/` containing:

- **Correlation ID middleware** — propagates `X-Correlation-Id` header across services
- **Structured logging** — Serilog configuration shared across all services
- **Health check base class** — standardized `/health` endpoint (liveness + readiness)
- **Exception handling middleware** — consistent error response format (RFC 7807 Problem Details)
- **HTTP client resilience** — Polly policies for retry, circuit breaker, timeout
- **Message broker abstractions** — `IEventPublisher` / `IEventConsumer` interfaces

#### 5.2.4 Message Broker Configuration

Set up RabbitMQ for local development via Docker Compose:

```yaml
# docker-compose.yml (excerpt)
services:
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"   # AMQP
      - "15672:15672" # Management UI
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
```

Define exchanges and queues:

| Exchange | Type | Binding Queue | Consumer |
|---|---|---|---|
| `product.events` | Topic | `inventory.product-events` | Inventory Service |
| `order.events` | Topic | `inventory.order-events` | Inventory Service |
| `inventory.events` | Topic | `order.inventory-events` | Order Service |

Use [MassTransit](https://masstransit-project.com/) as the .NET abstraction over RabbitMQ for publish/subscribe and saga support.

#### 5.2.5 Docker Compose Orchestration

```yaml
# docker-compose.yml
version: '3.8'
services:
  rabbitmq:
    image: rabbitmq:3-management
    ports: ["5672:5672", "15672:15672"]

  gateway:
    build: src/Gateway/ApiGateway
    ports: ["5100:8080"]
    depends_on: [customer-service, product-service, inventory-service, order-service]

  customer-service:
    build: src/Services/CustomerService.Api
    ports: ["5101:8080"]
    depends_on: [rabbitmq]

  product-service:
    build: src/Services/ProductCatalog.Api
    ports: ["5102:8080"]
    depends_on: [rabbitmq]

  inventory-service:
    build: src/Services/InventoryService.Api
    ports: ["5103:8080"]
    depends_on: [rabbitmq, product-service]

  order-service:
    build: src/Services/OrderService.Api
    ports: ["5104:8080"]
    depends_on: [rabbitmq, customer-service, product-service, inventory-service]
```

#### 5.2.6 CI/CD Pipeline Templates

Create `.github/workflows/` templates:

- **`service-build-template.yml`** — reusable workflow for `dotnet restore` → `dotnet build` → `dotnet test` → `dotnet format --verify-no-changes` → Docker build
- **`service-deploy-template.yml`** — reusable workflow for container registry push and deployment
- Per-service workflows that reference the templates

#### 5.2.7 Authentication / Authorization Approach

The monolith currently has no authentication. Define the target approach:

- API Gateway handles JWT validation (bearer token)
- Services trust the gateway and accept `X-User-Id` / `X-User-Roles` headers
- Shared auth middleware in `OrderManager.Common`

### 5.3 Outputs

- [ ] Solution file updated with all new projects
- [ ] `OrderManager.Contracts` project with all DTOs and event schemas
- [ ] `OrderManager.Common` project with middleware and utilities
- [ ] `docker-compose.yml` with RabbitMQ and service stubs
- [ ] Dockerfile template for each service
- [ ] CI/CD pipeline templates in `.github/workflows/`
- [ ] All new projects build successfully: `dotnet build`
- [ ] All existing tests still pass: `dotnet test`

### 5.4 Acceptance Criteria

1. `dotnet build OrderManager.sln` succeeds with zero errors
2. `dotnet test` passes all existing tests
3. `docker-compose up` starts RabbitMQ and all service stubs
4. Each service stub responds to `GET /health` with `200 OK`
5. `dotnet format --verify-no-changes` passes
6. Shared contracts compile and are referenceable from all service projects

---

## 6. Phase 1A — Customer Service Extraction

> **Type:** Parallel (with Phase 1B)
> **Child Session:** `Phase-1A-Customer-Service`
> **Dependencies:** Phase 0

### 6.1 Inputs

- Completed Phase 0 foundation (shared contracts, common library, solution structure)
- Source files to extract from monolith:
  - `src/OrderManager.Api/Models/Customer.cs`
  - `src/OrderManager.Api/Services/CustomerService.cs`
  - `src/OrderManager.Api/Controllers/CustomersController.cs`
  - Relevant seed data from `src/OrderManager.Api/Data/SeedData.cs` (lines 13-19)
  - Entity configuration from `src/OrderManager.Api/Data/AppDbContext.cs` (lines 18-24)

### 6.2 Tasks

#### 6.2.1 Create Customer Service Project

Create `src/Services/CustomerService.Api/` as a .NET 8 Web API project:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
    <PackageReference Include="MassTransit.RabbitMQ" Version="8.*" />
    <ProjectReference Include="../../Shared/OrderManager.Contracts/OrderManager.Contracts.csproj" />
    <ProjectReference Include="../../Shared/OrderManager.Common/OrderManager.Common.csproj" />
  </ItemGroup>
</Project>
```

#### 6.2.2 Extract Customer Entity

Copy `Customer` model to `src/Services/CustomerService.Api/Models/Customer.cs`:

```csharp
namespace CustomerService.Api.Models;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // REMOVED: public ICollection<Order> Orders — cross-domain navigation property
}
```

**Key change:** Remove the `Orders` navigation property. This was the cross-domain coupling identified in `Customer.cs:14`. Order history for a customer will be served by the Order Service API.

#### 6.2.3 Create Dedicated `CustomerDbContext`

```csharp
namespace CustomerService.Api.Data;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options) { }
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Extracted from AppDbContext.cs lines 18-24
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}
```

**Database:** `customer.db` (SQLite, separate from monolith's `ordermanager.db`)

#### 6.2.4 Extract Customer Service Logic

Copy `CustomerService.cs` logic into `src/Services/CustomerService.Api/Services/CustomerService.cs`:

- `GetAllCustomersAsync()` — unchanged (no cross-domain dependency)
- `GetCustomerByIdAsync(int id)` — **remove** `.Include(c => c.Orders)` (was on `CustomerService.cs:23`). Return customer data only. Order history is the Order Service's responsibility.
- `CreateCustomerAsync(Customer customer)` — unchanged, plus publish `CustomerCreatedEvent`

#### 6.2.5 Migrate Controller Endpoints

Recreate `CustomersController` in the new project, preserving exact route structure:

| Monolith Endpoint | New Service Endpoint | Changes |
|---|---|---|
| `GET /api/customers` | `GET /api/customers` | None |
| `GET /api/customers/{id}` | `GET /api/customers/{id}` | Remove orders from response |
| `POST /api/customers` | `POST /api/customers` | Add event publishing |

**New endpoint to add:**

| Endpoint | Purpose | Used By |
|---|---|---|
| `GET /api/customers/{id}/address` | Returns shipping address fields for order creation | Order Service (Phase 3) |

#### 6.2.6 Seed Data Migration

Extract customer seed data from `SeedData.cs:13-19`:

```csharp
public static class CustomerSeedData
{
    public static void Initialize(CustomerDbContext context)
    {
        context.Database.EnsureCreated();
        if (context.Customers.Any()) return;

        context.Customers.AddRange(
            new Customer { Name = "Acme Corp", Email = "orders@acme.com", Phone = "555-0100",
                Address = "123 Main St", City = "Springfield", State = "IL", ZipCode = "62701" },
            new Customer { Name = "Globex Inc", Email = "purchasing@globex.com", Phone = "555-0200",
                Address = "456 Oak Ave", City = "Shelbyville", State = "IL", ZipCode = "62565" },
            new Customer { Name = "Initech LLC", Email = "supplies@initech.com", Phone = "555-0300",
                Address = "789 Pine Rd", City = "Capital City", State = "IL", ZipCode = "62702" }
        );
        context.SaveChanges();
    }
}
```

#### 6.2.7 Event Publishing

On `CreateCustomerAsync`, publish:

```csharp
await _publishEndpoint.Publish(new CustomerCreatedEvent(customer.Id, customer.Name, customer.Email));
```

### 6.3 Outputs

- [ ] `src/Services/CustomerService.Api/` project with its own `CustomerDbContext`
- [ ] Customer model without `Orders` navigation property
- [ ] All three controller endpoints working and returning correct data
- [ ] New `GET /api/customers/{id}/address` endpoint
- [ ] Seed data creates 3 customers on first run
- [ ] `CustomerCreatedEvent` published on customer creation
- [ ] Dockerfile for the service
- [ ] Health check endpoint at `GET /health`

### 6.4 Acceptance Criteria

1. `dotnet build src/Services/CustomerService.Api/` succeeds
2. `dotnet test` (service-level tests) passes
3. `GET /api/customers` returns all 3 seeded customers
4. `GET /api/customers/1` returns customer without `orders` field
5. `GET /api/customers/1/address` returns `{ address, city, state, zipCode }`
6. `POST /api/customers` creates customer and publishes `CustomerCreatedEvent` to RabbitMQ
7. `dotnet format --verify-no-changes` passes
8. API contract matches `CustomerDto` from `OrderManager.Contracts`

---

## 7. Phase 1B — Product Catalog Service Extraction

> **Type:** Parallel (with Phase 1A)
> **Child Session:** `Phase-1B-Product-Catalog`
> **Dependencies:** Phase 0

### 7.1 Inputs

- Completed Phase 0 foundation
- Source files to extract from monolith:
  - `src/OrderManager.Api/Models/Product.cs`
  - `src/OrderManager.Api/Services/ProductService.cs`
  - `src/OrderManager.Api/Controllers/ProductsController.cs`
  - Relevant seed data from `src/OrderManager.Api/Data/SeedData.cs` (lines 21-30)
  - Entity configuration from `src/OrderManager.Api/Data/AppDbContext.cs` (lines 26-33)

### 7.2 Tasks

#### 7.2.1 Create Product Catalog Service Project

Create `src/Services/ProductCatalog.Api/` as a .NET 8 Web API project with the same package structure as Customer Service.

#### 7.2.2 Extract Product Entity

Copy `Product` model to `src/Services/ProductCatalog.Api/Models/Product.cs`:

```csharp
namespace ProductCatalog.Api.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Sku { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // REMOVED: public InventoryItem? Inventory — cross-domain navigation property
    // REMOVED: public ICollection<OrderItem> OrderItems — cross-domain navigation property
}
```

**Key changes:**
- Remove `Inventory` navigation property (`Product.cs:12`) — inventory data will come from Inventory Service API
- Remove `OrderItems` navigation property (`Product.cs:13`) — order line items belong to Order Service

#### 7.2.3 Create Dedicated `ProductDbContext`

```csharp
namespace ProductCatalog.Api.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Extracted from AppDbContext.cs lines 26-33
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Sku).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Sku).IsUnique();
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
        });
    }
}
```

**Database:** `product.db` (separate SQLite file)

#### 7.2.4 Extract Product Service Logic

Copy `ProductService.cs` logic with modifications:

- `GetAllProductsAsync()` — **remove** `.Include(p => p.Inventory)` (was on `ProductService.cs:18`). Return products without inventory data.
- `GetProductByIdAsync(int id)` — **remove** `.Include(p => p.Inventory)` (was on `ProductService.cs:23`).
- `GetProductsByCategoryAsync(string category)` — **remove** `.Include(p => p.Inventory)` (was on `ProductService.cs:35`).
- `CreateProductAsync(Product product)` — unchanged, plus publish `ProductCreatedEvent`

#### 7.2.5 Migrate Controller Endpoints

| Monolith Endpoint | New Service Endpoint | Changes |
|---|---|---|
| `GET /api/products` | `GET /api/products` | Remove inventory from response |
| `GET /api/products/{id}` | `GET /api/products/{id}` | Remove inventory from response |
| `GET /api/products/category/{category}` | `GET /api/products/category/{category}` | Remove inventory from response |
| `POST /api/products` | `POST /api/products` | Add event publishing |

**New endpoint to add:**

| Endpoint | Purpose | Used By |
|---|---|---|
| `GET /api/products/{id}/price` | Returns product price for order total calculation | Order Service (Phase 3) |

#### 7.2.6 Seed Data Migration

Extract product seed data from `SeedData.cs:21-30`:

```csharp
public static class ProductSeedData
{
    public static void Initialize(ProductDbContext context)
    {
        context.Database.EnsureCreated();
        if (context.Products.Any()) return;

        context.Products.AddRange(
            new Product { Name = "Widget A", Description = "Standard widget", Category = "Widgets", Price = 9.99m, Sku = "WGT-001" },
            new Product { Name = "Widget B", Description = "Premium widget", Category = "Widgets", Price = 19.99m, Sku = "WGT-002" },
            new Product { Name = "Gadget X", Description = "Basic gadget", Category = "Gadgets", Price = 29.99m, Sku = "GDG-001" },
            new Product { Name = "Gadget Y", Description = "Advanced gadget", Category = "Gadgets", Price = 49.99m, Sku = "GDG-002" },
            new Product { Name = "Thingamajig", Description = "Multi-purpose thingamajig", Category = "Misc", Price = 14.99m, Sku = "THG-001" }
        );
        context.SaveChanges();
    }
}
```

#### 7.2.7 Event Publishing

On `CreateProductAsync`, publish:

```csharp
await _publishEndpoint.Publish(new ProductCreatedEvent(product.Id, product.Name, product.Sku, product.Price));
```

On future `DeleteProductAsync` (to be added), publish:

```csharp
await _publishEndpoint.Publish(new ProductDeletedEvent(product.Id));
```

### 7.3 Outputs

- [ ] `src/Services/ProductCatalog.Api/` project with its own `ProductDbContext`
- [ ] Product model without `Inventory` and `OrderItems` navigation properties
- [ ] All four controller endpoints working and returning correct data
- [ ] New `GET /api/products/{id}/price` endpoint
- [ ] Seed data creates 5 products on first run
- [ ] `ProductCreatedEvent` published on product creation
- [ ] Dockerfile for the service
- [ ] Health check endpoint at `GET /health`

### 7.4 Acceptance Criteria

1. `dotnet build src/Services/ProductCatalog.Api/` succeeds
2. `dotnet test` passes
3. `GET /api/products` returns all 5 seeded products without `inventory` field
4. `GET /api/products/1` returns product without `inventory` field
5. `GET /api/products/category/Widgets` returns 2 products
6. `GET /api/products/1/price` returns `{ price: 9.99 }`
7. `POST /api/products` creates product and publishes `ProductCreatedEvent`
8. `dotnet format --verify-no-changes` passes
9. API contract matches `ProductDto` from `OrderManager.Contracts`

---

## 8. Phase 2 — Inventory Service Extraction

> **Type:** Sequential (depends on Phase 1B)
> **Child Session:** `Phase-2-Inventory-Service`
> **Dependencies:** Phase 1B (Product Catalog Service must be running)

### 8.1 Inputs

- Completed Phase 0 foundation
- Completed Phase 1B — Product Catalog Service API is available
- Source files to extract from monolith:
  - `src/OrderManager.Api/Models/InventoryItem.cs`
  - `src/OrderManager.Api/Services/InventoryService.cs`
  - `src/OrderManager.Api/Controllers/InventoryController.cs`
  - Relevant seed data from `src/OrderManager.Api/Data/SeedData.cs` (lines 32-40)
  - Entity configuration from `src/OrderManager.Api/Data/AppDbContext.cs` (lines 50-54)

### 8.2 Tasks

#### 8.2.1 Create Inventory Service Project

Create `src/Services/InventoryService.Api/` as a .NET 8 Web API project.

#### 8.2.2 Extract InventoryItem Entity

```csharp
namespace InventoryService.Api.Models;

public class InventoryItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }          // Stored reference — NO cross-DB FK
    // REMOVED: public Product Product — cross-domain navigation property
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; } = 10;
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime LastRestocked { get; set; } = DateTime.UtcNow;
}
```

**Key change:** Remove the `Product` navigation property (`InventoryItem.cs:7`). The `ProductId` is kept as a stored integer reference, but there is no cross-database foreign key constraint. Referential integrity is maintained via event consumption.

#### 8.2.3 Create Dedicated `InventoryDbContext`

```csharp
namespace InventoryService.Api.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Extracted from AppDbContext.cs lines 50-54
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Note: No .HasOne(e => e.Product) — cross-DB FK removed
            entity.HasIndex(e => e.ProductId).IsUnique(); // One inventory item per product
        });
    }
}
```

**Database:** `inventory.db` (separate SQLite file)

#### 8.2.4 Extract Inventory Service Logic

Copy `InventoryService.cs` logic with modifications:

- `GetAllInventoryAsync()` — **remove** `.Include(i => i.Product)` (was on `InventoryService.cs:18`). Return inventory items with `ProductId` only. Product details can be composed at the gateway.
- `GetInventoryByProductIdAsync(int productId)` — **remove** `.Include(i => i.Product)` (was on `InventoryService.cs:23`).
- `RestockAsync(int productId, int quantity)` — logic unchanged (`InventoryService.cs:26-34`), publishes event.
- `GetLowStockItemsAsync()` — **remove** `.Include(i => i.Product)` (was on `InventoryService.cs:39`).

**New methods to add:**

- `ReserveStockAsync(int productId, int quantity)` — atomically checks and decrements stock for order processing:

```csharp
public async Task<ReserveStockResponse> ReserveStockAsync(int productId, int quantity)
{
    var item = await _context.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId)
        ?? throw new ArgumentException($"No inventory record for product {productId}");

    if (item.QuantityOnHand < quantity)
        return new ReserveStockResponse(false, 0, item.QuantityOnHand);

    item.QuantityOnHand -= quantity;
    await _context.SaveChangesAsync();
    return new ReserveStockResponse(true, quantity, item.QuantityOnHand);
}
```

#### 8.2.5 Migrate Controller Endpoints

| Monolith Endpoint | New Service Endpoint | Changes |
|---|---|---|
| `GET /api/inventory` | `GET /api/inventory` | Remove product navigation from response |
| `GET /api/inventory/product/{productId}` | `GET /api/inventory/{productId}` | Remove product navigation, simplify route |
| `POST /api/inventory/product/{productId}/restock` | `POST /api/inventory/{productId}/restock` | Unchanged logic, add event |
| `GET /api/inventory/low-stock` | `GET /api/inventory/low-stock` | Remove product navigation |

**New endpoints to add:**

| Endpoint | Purpose | Used By |
|---|---|---|
| `POST /api/inventory/{productId}/reserve` | Reserve stock for an order | Order Service saga (Phase 3) |
| `POST /api/inventory/{productId}/release` | Release previously reserved stock (compensation) | Order Service saga (Phase 3) |

#### 8.2.6 Event Consumer — Product Lifecycle

Consume events from Product Catalog Service to maintain referential integrity:

```csharp
public class ProductCreatedEventConsumer : IConsumer<ProductCreatedEvent>
{
    public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
    {
        // Automatically create an inventory record for new products
        var item = new InventoryItem
        {
            ProductId = context.Message.ProductId,
            QuantityOnHand = 0,
            ReorderLevel = 10,
            WarehouseLocation = "UNASSIGNED"
        };
        // Save to InventoryDbContext
    }
}

public class ProductDeletedEventConsumer : IConsumer<ProductDeletedEvent>
{
    public async Task Consume(ConsumeContext<ProductDeletedEvent> context)
    {
        // Remove or soft-delete inventory record for deleted products
    }
}
```

#### 8.2.7 Event Publishing

On stock reservation:

```csharp
// Published when Order Service saga triggers stock reservation
await _publishEndpoint.Publish(new StockReservedEvent(orderId, reservedItems));
// or
await _publishEndpoint.Publish(new StockInsufficientEvent(orderId, productId, requested, available));
```

#### 8.2.8 Seed Data Migration

Extract inventory seed data from `SeedData.cs:32-40`, using known `ProductId` values:

```csharp
public static class InventorySeedData
{
    public static void Initialize(InventoryDbContext context)
    {
        context.Database.EnsureCreated();
        if (context.InventoryItems.Any()) return;

        // Product IDs must match the Product Catalog Service's seed data
        context.InventoryItems.AddRange(
            new InventoryItem { ProductId = 1, QuantityOnHand = 50, ReorderLevel = 10, WarehouseLocation = "A-01" },
            new InventoryItem { ProductId = 2, QuantityOnHand = 100, ReorderLevel = 10, WarehouseLocation = "A-02" },
            new InventoryItem { ProductId = 3, QuantityOnHand = 150, ReorderLevel = 10, WarehouseLocation = "A-03" },
            new InventoryItem { ProductId = 4, QuantityOnHand = 200, ReorderLevel = 10, WarehouseLocation = "A-04" },
            new InventoryItem { ProductId = 5, QuantityOnHand = 250, ReorderLevel = 10, WarehouseLocation = "A-05" }
        );
        context.SaveChanges();
    }
}
```

### 8.3 Outputs

- [ ] `src/Services/InventoryService.Api/` project with its own `InventoryDbContext`
- [ ] InventoryItem model without `Product` navigation property
- [ ] All four original controller endpoints working
- [ ] New `POST /api/inventory/{productId}/reserve` endpoint
- [ ] New `POST /api/inventory/{productId}/release` endpoint
- [ ] `ProductCreatedEvent` / `ProductDeletedEvent` consumers active
- [ ] `StockReservedEvent` / `StockInsufficientEvent` published on reservation
- [ ] Seed data creates 5 inventory items on first run
- [ ] Dockerfile and health check

### 8.4 Acceptance Criteria

1. `dotnet build src/Services/InventoryService.Api/` succeeds
2. `dotnet test` passes
3. `GET /api/inventory` returns all 5 seeded items without `product` nested object
4. `POST /api/inventory/1/reserve` with `{ "quantity": 5 }` returns success and decrements stock
5. `POST /api/inventory/1/reserve` with quantity exceeding stock returns `{ "success": false }`
6. `POST /api/inventory/1/release` restores previously reserved stock
7. Publishing a `ProductCreatedEvent` to RabbitMQ creates a new inventory record
8. `dotnet format --verify-no-changes` passes
9. API contract matches `InventoryItemDto` from `OrderManager.Contracts`

---

## 9. Phase 3 — Order Service Extraction + Saga

> **Type:** Sequential
> **Child Session:** `Phase-3-Order-Service-Saga`
> **Dependencies:** Phases 1A, 1B, and 2 (all upstream services must be available)

### 9.1 Inputs

- Completed Phases 0, 1A, 1B, 2 — all upstream service APIs available
- Source files to extract from monolith:
  - `src/OrderManager.Api/Models/Order.cs`
  - `src/OrderManager.Api/Models/OrderItem.cs`
  - `src/OrderManager.Api/Services/OrderService.cs` (**critical refactor target**)
  - `src/OrderManager.Api/Controllers/OrdersController.cs`
  - Entity configuration from `src/OrderManager.Api/Data/AppDbContext.cs` (lines 35-48)

### 9.2 Tasks

#### 9.2.1 Create Order Service Project

Create `src/Services/OrderService.Api/` as a .NET 8 Web API project with MassTransit saga support:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
  <PackageReference Include="MassTransit.RabbitMQ" Version="8.*" />
  <PackageReference Include="Polly" Version="8.*" />
  <ProjectReference Include="../../Shared/OrderManager.Contracts/OrderManager.Contracts.csproj" />
  <ProjectReference Include="../../Shared/OrderManager.Common/OrderManager.Common.csproj" />
</ItemGroup>
```

#### 9.2.2 Extract Order and OrderItem Entities

```csharp
namespace OrderService.Api.Models;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }     // Stored reference — NO cross-DB FK
    // REMOVED: public Customer Customer — cross-domain navigation property
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";  // Pending → Confirmed → Failed → Cancelled
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductId { get; set; }      // Stored reference — NO cross-DB FK
    // REMOVED: public Product Product — cross-domain navigation property
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}
```

**Key changes:**
- Remove `Customer` navigation property from `Order` (`Order.cs:7`)
- Remove `Product` navigation property from `OrderItem` (`OrderItem.cs:9`)
- Keep `CustomerId` and `ProductId` as stored integer references
- Add richer `Status` values to support saga states

#### 9.2.3 Create Dedicated `OrderDbContext`

```csharp
namespace OrderService.Api.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Extracted from AppDbContext.cs lines 35-48
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Note: No .HasOne(e => e.Customer) — cross-DB FK removed
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Order).WithMany(o => o.Items).HasForeignKey(e => e.OrderId);
            // Note: No .HasOne(e => e.Product) — cross-DB FK removed
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
        });
    }
}
```

**Database:** `order.db` (separate SQLite file)

#### 9.2.4 Refactor `CreateOrderAsync` — Saga Implementation

This is the **critical change**. The current `OrderService.CreateOrderAsync` (`OrderService.cs:33-69`) performs everything in a single database transaction across four domains. This must become a distributed saga.

**Current monolith flow (single transaction):**

```
1. _context.Customers.FindAsync(customerId)          → Read Customer
2. Build ShippingAddress from customer fields          → Read Customer
3. For each item:
   a. _context.Products.FindAsync(productId)          → Read Product
   b. _context.InventoryItems.FirstOrDefault(...)     → Read Inventory
   c. Check inventory.QuantityOnHand >= quantity       → Validate
   d. inventory.QuantityOnHand -= quantity             → Write Inventory
   e. Create OrderItem with product.Price              → Write Order
4. order.TotalAmount = sum of items                    → Compute
5. _context.Orders.Add(order)                          → Write Order
6. _context.SaveChangesAsync()                         → Single COMMIT
```

**New saga flow (distributed):**

```
Step 1: Validate Customer (HTTP → Customer Service)
  POST creates order with Status = "Pending"
  GET /api/customers/{customerId}
  → If not found: return 404, set Status = "Failed"
  GET /api/customers/{customerId}/address
  → Store shipping address on order

Step 2: Get Product Prices (HTTP → Product Catalog Service)
  For each line item:
    GET /api/products/{productId}/price
    → If not found: set Status = "Failed", abort
    → Store UnitPrice on OrderItem

Step 3: Reserve Stock (Event → Inventory Service)
  Publish OrderPlacedEvent with line items
  Inventory Service consumes event:
    For each item: POST /api/inventory/{productId}/reserve
    → If all succeed: publish StockReservedEvent
    → If any fail: publish StockInsufficientEvent
      (Inventory Service rolls back any partial reservations)

Step 4: Confirm or Fail Order (Event consumer)
  Order Service consumes StockReservedEvent:
    → Set Status = "Confirmed", compute TotalAmount
  Order Service consumes StockInsufficientEvent:
    → Set Status = "Failed"
    → Record failure reason
```

**Implementation using MassTransit Saga State Machine:**

```csharp
public class OrderSaga : MassTransitStateMachine<OrderSagaState>
{
    public State Pending { get; private set; }
    public State AwaitingStockReservation { get; private set; }
    public State Confirmed { get; private set; }
    public State Failed { get; private set; }

    public Event<OrderPlacedEvent> OrderPlaced { get; private set; }
    public Event<StockReservedEvent> StockReserved { get; private set; }
    public Event<StockInsufficientEvent> StockInsufficient { get; private set; }

    public OrderSaga()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderPlaced, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => StockReserved, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => StockInsufficient, x => x.CorrelateById(m => m.Message.OrderId));

        Initially(
            When(OrderPlaced)
                .TransitionTo(AwaitingStockReservation));

        During(AwaitingStockReservation,
            When(StockReserved)
                .Then(context => { /* Mark order as Confirmed */ })
                .TransitionTo(Confirmed)
                .Finalize(),
            When(StockInsufficient)
                .Then(context => { /* Mark order as Failed, log reason */ })
                .TransitionTo(Failed)
                .Finalize());
    }
}
```

#### 9.2.5 HTTP Client Services for Upstream Calls

```csharp
// Typed HTTP clients with Polly resilience
public class CustomerApiClient
{
    private readonly HttpClient _http;

    public async Task<CustomerDto?> GetCustomerAsync(int customerId)
        => await _http.GetFromJsonAsync<CustomerDto>($"/api/customers/{customerId}");

    public async Task<CustomerAddressDto?> GetCustomerAddressAsync(int customerId)
        => await _http.GetFromJsonAsync<CustomerAddressDto>($"/api/customers/{customerId}/address");
}

public class ProductApiClient
{
    private readonly HttpClient _http;

    public async Task<ProductPriceDto?> GetProductPriceAsync(int productId)
        => await _http.GetFromJsonAsync<ProductPriceDto>($"/api/products/{productId}/price");
}
```

Register with Polly resilience:

```csharp
builder.Services.AddHttpClient<CustomerApiClient>(client =>
    client.BaseAddress = new Uri("http://customer-service:8080"))
    .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(300)))
    .AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
```

#### 9.2.6 Idempotency

Add an `IdempotencyKey` field to the order creation flow:

```csharp
public class Order
{
    // ... existing fields
    public string? IdempotencyKey { get; set; }  // Client-provided UUID
}
```

Before processing `CreateOrderAsync`, check if an order with the same `IdempotencyKey` already exists. If so, return the existing order.

#### 9.2.7 Migrate Controller Endpoints

| Monolith Endpoint | New Service Endpoint | Changes |
|---|---|---|
| `GET /api/orders` | `GET /api/orders` | Remove `Customer` and `Product` nested objects |
| `GET /api/orders/{id}` | `GET /api/orders/{id}` | Remove navigation properties; return IDs only |
| `POST /api/orders` | `POST /api/orders` | Saga-based async creation; returns `202 Accepted` with order ID |
| `PATCH /api/orders/{id}/status` | `PATCH /api/orders/{id}/status` | Unchanged |

**New endpoints:**

| Endpoint | Purpose |
|---|---|
| `GET /api/orders/customer/{customerId}` | Get orders for a customer (replaces the `Include(c => c.Orders)` from Customer Service) |
| `GET /api/orders/{id}/status` | Poll order status during async processing |

### 9.3 Outputs

- [ ] `src/Services/OrderService.Api/` project with its own `OrderDbContext`
- [ ] Order and OrderItem models without cross-domain navigation properties
- [ ] MassTransit saga state machine for order creation flow
- [ ] HTTP clients for Customer Service and Product Catalog Service with Polly resilience
- [ ] `StockReservedEvent` / `StockInsufficientEvent` consumers
- [ ] Idempotency key support on order creation
- [ ] All controller endpoints working
- [ ] New `GET /api/orders/customer/{customerId}` endpoint
- [ ] Dockerfile and health check

### 9.4 Acceptance Criteria

1. `dotnet build src/Services/OrderService.Api/` succeeds
2. `dotnet test` passes
3. `POST /api/orders` with valid customer and products returns `202 Accepted`
4. Order transitions from `Pending` → `Confirmed` when stock is available
5. Order transitions from `Pending` → `Failed` when stock is insufficient
6. Duplicate `POST` with same `IdempotencyKey` returns existing order (not duplicate)
7. `GET /api/orders/customer/1` returns orders for customer 1
8. Compensation: if stock reservation fails mid-way, previously reserved items are released
9. `dotnet format --verify-no-changes` passes
10. All events published/consumed correctly via RabbitMQ

---

## 10. Phase 4A — API Gateway Setup

> **Type:** Parallel (with Phase 4B)
> **Child Session:** `Phase-4A-API-Gateway`
> **Dependencies:** Phase 3

### 10.1 Inputs

- All four microservices running with their APIs
- `OrderManager.Contracts` defining the API surface

### 10.2 Tasks

#### 10.2.1 Create YARP-Based API Gateway

Create `src/Gateway/ApiGateway/` using [YARP (Yet Another Reverse Proxy)](https://microsoft.github.io/reverse-proxy/):

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
var app = builder.Build();
app.MapReverseProxy();
app.Run();
```

#### 10.2.2 Route Configuration

```json
{
  "ReverseProxy": {
    "Routes": {
      "customer-route": {
        "ClusterId": "customer-cluster",
        "Match": { "Path": "/api/customers/{**catch-all}" }
      },
      "product-route": {
        "ClusterId": "product-cluster",
        "Match": { "Path": "/api/products/{**catch-all}" }
      },
      "inventory-route": {
        "ClusterId": "inventory-cluster",
        "Match": { "Path": "/api/inventory/{**catch-all}" }
      },
      "order-route": {
        "ClusterId": "order-cluster",
        "Match": { "Path": "/api/orders/{**catch-all}" }
      }
    },
    "Clusters": {
      "customer-cluster": { "Destinations": { "d1": { "Address": "http://customer-service:8080" } } },
      "product-cluster": { "Destinations": { "d1": { "Address": "http://product-service:8080" } } },
      "inventory-cluster": { "Destinations": { "d1": { "Address": "http://inventory-service:8080" } } },
      "order-cluster": { "Destinations": { "d1": { "Address": "http://order-service:8080" } } }
    }
  }
}
```

#### 10.2.3 API Composition for Aggregate Views

The monolith's `GET /api/products` currently returns products with embedded inventory data (via `Include(p => p.Inventory)`). The gateway needs to compose this view:

```csharp
// Composition middleware for GET /api/products
app.MapGet("/api/products/with-inventory", async (
    HttpClient productClient, HttpClient inventoryClient) =>
{
    var products = await productClient.GetFromJsonAsync<List<ProductDto>>("/api/products");
    var inventory = await inventoryClient.GetFromJsonAsync<List<InventoryItemDto>>("/api/inventory");

    var composed = products.Select(p => new
    {
        p.Id, p.Name, p.Description, p.Category, p.Price, p.Sku, p.CreatedAt,
        Inventory = inventory.FirstOrDefault(i => i.ProductId == p.Id)
    });

    return Results.Ok(composed);
});
```

Similarly for `GET /api/orders` (compose customer names) and `GET /api/customers/{id}` (compose order history).

#### 10.2.4 Gateway Features

- **Correlation ID injection** — generate or forward `X-Correlation-Id` header
- **Rate limiting** — using `Microsoft.AspNetCore.RateLimiting`
- **Response caching** — for read-heavy endpoints like product catalog
- **Health check aggregation** — `GET /health` that checks all downstream services
- **CORS configuration** — matches current monolith CORS policy (allow any origin)
- **Static files** — serve Angular frontend from gateway (same as monolith's `app.UseStaticFiles()`)

### 10.3 Outputs

- [ ] `src/Gateway/ApiGateway/` project with YARP configuration
- [ ] All `/api/*` routes proxied to correct services
- [ ] Composition endpoints for aggregate views
- [ ] Correlation ID propagation
- [ ] Aggregated health check
- [ ] CORS configured
- [ ] Static file serving for Angular frontend
- [ ] Dockerfile

### 10.4 Acceptance Criteria

1. `dotnet build src/Gateway/ApiGateway/` succeeds
2. `GET http://gateway:5100/api/customers` proxied to Customer Service
3. `GET http://gateway:5100/api/products` proxied to Product Catalog Service
4. `GET http://gateway:5100/api/products/with-inventory` returns composed data
5. `GET http://gateway:5100/health` returns status of all downstream services
6. Angular frontend loads from gateway root URL
7. `dotnet format --verify-no-changes` passes

---

## 11. Phase 4B — Angular Frontend Migration

> **Type:** Parallel (with Phase 4A)
> **Child Session:** `Phase-4B-Angular-Frontend`
> **Dependencies:** Phase 3

### 11.1 Inputs

- All four microservices running
- API Gateway configured (or known route structure)
- Angular frontend at `client-app/`

### 11.2 Tasks

#### 11.2.1 Update API Base URL

Currently all Angular components call `/api/*` directly. This should continue to work when the Angular app is served by the API Gateway. However, add an environment-based API base URL for flexibility:

```typescript
// environment.ts
export const environment = {
  production: false,
  apiBaseUrl: '/api'  // Relative URL — works when served by gateway
};
```

#### 11.2.2 Handle Eventual Consistency for Orders

The monolith's `POST /api/orders` returns the completed order synchronously. With the saga pattern, the order creation is asynchronous. Update the Angular order flow:

```typescript
// order-list.component.ts — updated order creation flow
async createOrder(request: CreateOrderRequest) {
  // POST returns 202 with order ID
  const { orderId } = await this.http.post<{ orderId: number }>('/api/orders', request).toPromise();

  // Poll for order status
  this.pollOrderStatus(orderId);
}

private pollOrderStatus(orderId: number) {
  const interval = setInterval(async () => {
    const order = await this.http.get<Order>(`/api/orders/${orderId}`).toPromise();
    if (order.status === 'Confirmed' || order.status === 'Failed') {
      clearInterval(interval);
      this.refreshOrders();

      if (order.status === 'Failed') {
        // Show user-friendly error
      }
    }
  }, 1000); // Poll every second
}
```

#### 11.2.3 Handle Missing Navigation Properties in Responses

The monolith's API responses include nested objects (e.g., order includes `customer.name`, product includes `inventory.quantityOnHand`). The microservices return flat data with IDs only.

**Option A (recommended for now):** Use the gateway's composition endpoints (`/api/products/with-inventory`) which maintain the same response shape.

**Option B (long-term):** Update Angular components to make parallel API calls and compose data client-side.

For the initial migration, update the Angular components to use the composed gateway endpoints:

| Current API Call | Gateway Endpoint | Change Required |
|---|---|---|
| `GET /api/orders` | `GET /api/orders` (composed at gateway) | Minimal — gateway adds customer name |
| `GET /api/products` | `GET /api/products/with-inventory` (composed) | Update URL |
| `GET /api/customers` | `GET /api/customers` | None |
| `GET /api/inventory` | `GET /api/inventory` | None |

#### 11.2.4 Update `product-list.component.ts`

```typescript
// Before (monolith)
ngOnInit() { this.http.get<any[]>('/api/products').subscribe(data => this.products = data); }

// After (microservices — use composed endpoint)
ngOnInit() { this.http.get<any[]>('/api/products/with-inventory').subscribe(data => this.products = data); }
```

#### 11.2.5 Add Loading States and Error Handling

With distributed services, network calls may be slower or fail individually. Add:

- Loading spinners during API calls
- Error boundaries for individual service failures
- Retry buttons for failed requests
- Toast notifications for order status changes

### 11.3 Outputs

- [ ] Angular `environment.ts` with configurable API base URL
- [ ] Order creation flow handles async saga (polling or WebSocket)
- [ ] Product list uses composed gateway endpoint
- [ ] Loading states and error handling added
- [ ] All four views render correctly with microservices backend

### 11.4 Acceptance Criteria

1. `cd client-app && npm install && npx ng build` succeeds
2. Customer list renders with data from Customer Service
3. Product list renders with inventory counts from composed endpoint
4. Inventory list renders with data from Inventory Service
5. Order list renders with customer names from composed endpoint
6. Creating an order shows pending state, then updates to confirmed/failed
7. No console errors in browser developer tools
8. All existing functionality preserved

---

## 12. Phase 5 — Decommission Monolith

> **Type:** Sequential
> **Child Session:** `Phase-5-Decommission`
> **Dependencies:** Phases 4A, 4B

### 12.1 Inputs

- All microservices running and handling traffic via API Gateway
- Angular frontend updated and verified
- Monolith still running in parallel (strangler fig pattern)

### 12.2 Tasks

#### 12.2.1 Feature Flag Setup

Implement a feature flag system to gradually route traffic:

```csharp
// Gateway configuration
"FeatureFlags": {
  "UseCustomerMicroservice": true,
  "UseProductMicroservice": true,
  "UseInventoryMicroservice": true,
  "UseOrderMicroservice": true
}
```

When a flag is `false`, the gateway routes to the monolith instead.

#### 12.2.2 Data Migration

Migrate data from the monolith's `ordermanager.db` to individual service databases:

| Source Table | Target Service | Target Database | Migration Script |
|---|---|---|---|
| `Customers` | Customer Service | `customer.db` | `scripts/migrate-customers.sql` |
| `Products` | Product Catalog Service | `product.db` | `scripts/migrate-products.sql` |
| `InventoryItems` | Inventory Service | `inventory.db` | `scripts/migrate-inventory.sql` |
| `Orders` + `OrderItems` | Order Service | `order.db` | `scripts/migrate-orders.sql` |

Migration steps:
1. Put monolith in read-only mode
2. Export data from `ordermanager.db`
3. Import into individual service databases with ID preservation
4. Verify row counts and data integrity
5. Switch feature flags to route all traffic to microservices

#### 12.2.3 Traffic Migration

Gradual rollout strategy:

1. **Shadow mode** — duplicate requests to both monolith and microservice, compare responses
2. **Canary (10%)** — route 10% of traffic to microservices, monitor errors
3. **Canary (50%)** — increase to 50%
4. **Full cutover (100%)** — route all traffic to microservices
5. **Monitoring period** — keep monolith running but idle for 2 weeks
6. **Decommission** — remove monolith from deployment

#### 12.2.4 Cleanup

- Remove `src/OrderManager.Api/` from solution (archive, don't delete git history)
- Remove monolith Docker service from `docker-compose.yml`
- Update `README.md` with new architecture documentation
- Update CI/CD pipelines to remove monolith build steps

### 12.3 Outputs

- [ ] Feature flag configuration for gradual traffic shifting
- [ ] Data migration scripts for each service database
- [ ] Shadow mode testing results
- [ ] Traffic cutover completed
- [ ] Monolith removed from active deployment
- [ ] Updated documentation

### 12.4 Acceptance Criteria

1. All data migrated with zero data loss (verified by row count and checksum)
2. All API endpoints return identical results from microservices vs monolith (validated in shadow mode)
3. Zero downtime during traffic cutover
4. Monolith can be reactivated by flipping feature flags (rollback capability)
5. All microservices healthy for 2-week monitoring period
6. Monolith removed from active deployment configuration

---

## 13. Cross-Cutting Concerns

### 13.1 Observability

| Concern | Tool | Implementation |
|---|---|---|
| Distributed tracing | OpenTelemetry + Jaeger | Correlation IDs propagated via headers |
| Metrics | Prometheus + Grafana | ASP.NET Core metrics exported |
| Logging | Serilog + Seq/ELK | Structured logging with correlation IDs |
| Health monitoring | ASP.NET Health Checks | `/health` endpoint per service |

### 13.2 Resilience Patterns

| Pattern | Implementation | Used In |
|---|---|---|
| Retry with exponential backoff | Polly `WaitAndRetryAsync` | All HTTP clients |
| Circuit breaker | Polly `CircuitBreakerAsync` | All HTTP clients |
| Timeout | Polly `TimeoutAsync` | All HTTP clients |
| Bulkhead isolation | Polly `BulkheadAsync` | Order Service (prevent cascading failures) |
| Idempotency | `IdempotencyKey` field | Order creation |
| Compensation | Saga rollback | Order Service saga |

### 13.3 Data Consistency

| Scenario | Pattern | Details |
|---|---|---|
| Order creation | Saga (orchestrated) | MassTransit state machine |
| Product → Inventory sync | Event-driven | `ProductCreatedEvent` → auto-create inventory record |
| Cross-service reads | API composition | Gateway composes responses from multiple services |
| Referential integrity | Eventual consistency | Events maintain cross-service references |

### 13.4 Testing Strategy

| Level | Scope | Tools |
|---|---|---|
| Unit tests | Individual service logic | xUnit, Moq |
| Integration tests | Service + database | `WebApplicationFactory`, TestContainers |
| Contract tests | API surface preservation | Pact or custom OpenAPI diff |
| End-to-end tests | Full flow through gateway | Playwright, HttpClient |

**Contract test principle:** Every PR must verify that the API surface is preserved. The exact API routes and response shapes from the monolith must be maintained (or composed at the gateway) so the Angular frontend continues working without changes.

---

## 14. Risk Register

| Risk | Impact | Probability | Mitigation |
|---|---|---|---|
| Data inconsistency during saga failures | High | Medium | Implement compensating transactions; test all failure paths |
| Increased latency from inter-service HTTP calls | Medium | High | Use caching at gateway; batch requests; consider gRPC for hot paths |
| Breaking Angular frontend during migration | High | Medium | Maintain identical API routes at gateway; use feature flags for gradual rollout |
| Message broker becomes single point of failure | High | Low | RabbitMQ clustering; dead letter queues; circuit breakers |
| Seed data ID mismatch across services | Medium | Medium | Use deterministic IDs in seed data; validate in integration tests |
| Increased operational complexity | Medium | High | Docker Compose for local dev; comprehensive health checks; centralized logging |
| Database migration data loss | Critical | Low | Backup before migration; checksums; dry-run scripts; rollback capability |

---

*This document serves as the orchestration plan for the monolith decomposition. Each phase section is designed to be independently executable as a child session with clear inputs, outputs, and acceptance criteria.*
