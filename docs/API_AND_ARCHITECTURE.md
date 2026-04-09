# OrderManager API & Architecture Documentation

## Table of Contents

1. [Overview](#overview)
2. [Technology Stack](#technology-stack)
3. [Project Structure](#project-structure)
4. [Architecture Patterns](#architecture-patterns)
5. [Entity-Relationship Model](#entity-relationship-model)
6. [REST API Reference](#rest-api-reference)
7. [Data Transfer Objects (DTOs)](#data-transfer-objects-dtos)
8. [Configuration](#configuration)
9. [Data Seeding](#data-seeding)
10. [Testing Strategy](#testing-strategy)
11. [Running the Application](#running-the-application)
12. [.NET to Java Migration Reference](#net-to-java-migration-reference)

---

## Overview

OrderManager is a full-stack monolithic application for managing products, customers, inventory, and orders. The backend is built with **Java 17 / Spring Boot 3.2** and the frontend uses **Angular 17**. Both are served from a single deployable unit -- the Spring Boot server hosts the Angular SPA as static resources on port `5001`.

---

## Technology Stack

| Layer          | Technology                        | Version  |
|----------------|-----------------------------------|----------|
| Language       | Java                              | 17       |
| Framework      | Spring Boot                       | 3.2.0    |
| ORM            | Spring Data JPA (Hibernate)       | 6.x      |
| Database       | H2 (file-based)                   | Runtime  |
| API Docs       | SpringDoc OpenAPI (Swagger UI)    | 2.3.0    |
| Build Tool     | Apache Maven                      | 3.x      |
| Frontend       | Angular                           | 17       |
| Testing        | JUnit 5, Spring Boot Test         | 5.x      |

### Maven Dependencies

```xml
spring-boot-starter-web          -- Embedded Tomcat, Spring MVC, Jackson JSON
spring-boot-starter-data-jpa     -- Hibernate ORM, Spring Data repositories
spring-boot-starter-validation   -- Bean Validation API (JSR 380)
h2                               -- Embedded SQL database (runtime scope)
springdoc-openapi-starter-webmvc-ui  -- Swagger UI + OpenAPI spec generation
spring-boot-starter-test         -- JUnit 5, Mockito, Spring Test (test scope)
```

---

## Project Structure

```
server/
  src/main/java/com/ordermanager/api/
    OrderManagerApplication.java          # Spring Boot entry point
    config/
      CorsConfig.java                     # CORS filter (allows all origins)
      DataSeeder.java                     # Seeds demo data on startup
      WebConfig.java                      # SPA fallback for Angular routing
    controller/
      CustomersController.java            # /api/customers endpoints
      InventoryController.java            # /api/inventory endpoints
      OrdersController.java              # /api/orders endpoints
      ProductsController.java             # /api/products endpoints
    dto/
      CreateOrderRequest.java             # POST /api/orders request body
      OrderItemRequest.java               # Line item within CreateOrderRequest
      RestockRequest.java                 # POST /api/inventory/.../restock body
      UpdateStatusRequest.java            # PATCH /api/orders/{id}/status body
    model/
      Customer.java                       # JPA entity
      CustomerOrder.java                  # JPA entity (renamed from "Order")
      InventoryItem.java                  # JPA entity
      OrderItem.java                      # JPA entity
      Product.java                        # JPA entity
    repository/
      CustomerRepository.java             # Spring Data JPA interface
      InventoryItemRepository.java        # Spring Data JPA interface
      OrderItemRepository.java            # Spring Data JPA interface
      OrderRepository.java               # Spring Data JPA interface
      ProductRepository.java              # Spring Data JPA interface
    service/
      CustomerService.java                # Business logic
      InventoryService.java               # Business logic + restock/low-stock
      OrderService.java                   # Business logic + order creation
      ProductService.java                 # Business logic
  src/main/resources/
    application.properties                # App configuration
    static/                               # Angular build output (served as SPA)
  src/test/
    java/.../service/OrderServiceTest.java  # Integration tests
    resources/application-test.properties   # Test-specific config

client-app/                               # Angular 17 SPA source
  src/
    app/
      modules/
        orders/                           # Orders page components
        products/                         # Products page components
        customers/                        # Customers page components
        inventory/                        # Inventory page components
```

---

## Architecture Patterns

### 1. Layered Architecture (Controller - Service - Repository)

The application follows the classic **three-tier layered architecture**:

```
  HTTP Request
       |
       v
  +-----------------+
  |   Controller    |   @RestController -- Handles HTTP, validates input,
  |   (API Layer)   |   maps DTOs, returns ResponseEntity
  +-----------------+
       |
       v
  +-----------------+
  |    Service      |   @Service -- Business logic, transaction management,
  |  (Business)     |   cross-entity coordination
  +-----------------+
       |
       v
  +-----------------+
  |   Repository    |   JpaRepository<T, ID> -- Data access via Spring Data
  |  (Data Access)  |   auto-generated CRUD + custom query methods
  +-----------------+
       |
       v
  +-----------------+
  |    H2 Database  |   File-based (./ordermanager.mv.db)
  +-----------------+
```

**Key principles:**
- Controllers never access repositories directly -- all business logic goes through services.
- Services are annotated with `@Transactional(readOnly = true)` at the class level; mutating methods override with `@Transactional`.
- Repositories are Spring Data JPA interfaces that auto-generate implementations at runtime.

### 2. Constructor Injection (Dependency Injection)

All dependencies are injected via **constructor injection** (no `@Autowired` on fields):

```java
@Service
public class OrderService {
    private final OrderRepository orderRepository;
    private final CustomerRepository customerRepository;
    // ...

    public OrderService(OrderRepository orderRepository,
                        CustomerRepository customerRepository, ...) {
        this.orderRepository = orderRepository;
        this.customerRepository = customerRepository;
    }
}
```

This pattern ensures:
- Immutable dependencies (`final` fields)
- Easier unit testing (dependencies can be mocked in constructors)
- Explicit dependency declaration

### 3. Repository Pattern (Spring Data JPA)

Each entity has a corresponding repository interface extending `JpaRepository<T, ID>`:

```java
@Repository
public interface ProductRepository extends JpaRepository<Product, Integer> {
    List<Product> findByCategory(String category);
}
```

Spring Data JPA auto-generates implementations for:
- Standard CRUD: `save()`, `findById()`, `findAll()`, `deleteById()`, `count()`
- Custom derived queries: method names are parsed into SQL (e.g., `findByCategory` -> `WHERE category = ?`)
- Custom sort queries: `findAllByOrderByOrderDateDesc()`

### 4. DTO Pattern (Request/Response Separation)

Write operations use **dedicated DTO classes** to decouple the API contract from the JPA entity:

| DTO                   | Used By                          | Purpose                           |
|-----------------------|----------------------------------|-----------------------------------|
| `CreateOrderRequest`  | `POST /api/orders`               | Captures customerId + line items  |
| `OrderItemRequest`    | Nested in `CreateOrderRequest`   | Single line item (productId, qty) |
| `RestockRequest`      | `POST /api/inventory/.../restock`| Quantity to add                   |
| `UpdateStatusRequest` | `PATCH /api/orders/{id}/status`  | New status string                 |

Read operations return JPA entities directly, with Jackson serialization controlled by `@JsonIgnoreProperties` and `@JsonIgnore` annotations.

### 5. JSON Serialization & Cycle Breaking

Bidirectional JPA relationships (e.g., `Customer <-> CustomerOrder`) would cause infinite recursion during JSON serialization. This is solved using `@JsonIgnoreProperties`:

```
Customer.orders          --> @JsonIgnoreProperties({"customer"})
CustomerOrder.customer   --> @JsonIgnoreProperties({"orders"})
CustomerOrder.items      --> @JsonIgnoreProperties({"order"})
OrderItem.order          --> @JsonIgnoreProperties({"items", "customer"})
OrderItem.product        --> @JsonIgnoreProperties({"inventory", "orderItems"})
InventoryItem.product    --> @JsonIgnoreProperties({"inventory", "orderItems"})
Product.inventory        --> @JsonIgnoreProperties({"product"})
Product.orderItems       --> @JsonIgnore (completely hidden)
```

This approach ensures related data is included in responses (e.g., customer name on an order, product name on inventory) while preventing infinite loops.

### 6. Eager Fetching Strategy

Since `spring.jpa.open-in-view=false` (no lazy loading outside transactions), key relationships use `FetchType.EAGER`:

| Relationship                           | Fetch Type | Reason                                         |
|----------------------------------------|------------|-------------------------------------------------|
| `CustomerOrder.customer`               | EAGER      | Customer name needed in order JSON response     |
| `CustomerOrder.items`                  | EAGER      | Line items needed when displaying an order      |
| `Customer.orders`                      | EAGER      | Orders list needed on customer detail view       |
| `InventoryItem.product`               | EAGER      | Product name needed on inventory list            |
| `OrderItem.product`                   | EAGER      | Product name needed on order line items          |
| `OrderItem.order`                     | LAZY       | Excluded from JSON via `@JsonIgnoreProperties`   |
| `Product.orderItems`                  | Default    | Hidden via `@JsonIgnore`                         |

> **Note:** Eager fetching is acceptable for this demo application with small datasets. For production systems with large data volumes, consider using `@EntityGraph`, projections, or DTOs for read operations.

### 7. SPA Fallback (Angular Routing Support)

`WebConfig` implements a custom `PathResourceResolver` that serves `index.html` for any request that doesn't match a static file:

```java
@Override
protected Resource getResource(String resourcePath, Resource location) throws IOException {
    Resource requestedResource = location.createRelative(resourcePath);
    if (requestedResource.exists() && requestedResource.isReadable()) {
        return requestedResource;  // Serve the actual file (JS, CSS, images)
    }
    return new ClassPathResource("/static/index.html");  // SPA fallback
}
```

This allows Angular's client-side router to handle routes like `/products`, `/customers`, etc., even on direct navigation or page refresh.

### 8. Transaction Management

- **Class-level `@Transactional(readOnly = true)`** on all service classes -- optimizes read operations with read-only transactions.
- **Method-level `@Transactional`** on mutating methods (create, update, restock) -- enables write transactions with automatic rollback on unchecked exceptions.
- `OrderService.createOrder()` performs multiple writes (order, order items, inventory deduction) within a single transaction, ensuring atomicity.

### 9. Monolith SPA Architecture

```
                    Port 5001
                       |
         +-------------+-------------+
         |                           |
    /api/*                      All other paths
         |                           |
    Spring MVC               WebConfig SPA Fallback
    REST Controllers              |
         |                   Angular SPA
    Service Layer            (index.html + JS bundles)
         |
    Spring Data JPA
         |
    H2 Database
```

Both the API and frontend are served from the same origin, eliminating CORS issues in production. The `CorsConfig` is included for development convenience when running Angular's dev server separately.

---

## Entity-Relationship Model

```
  +------------+       +----------------+       +-------------+
  |  Customer  |1----*|  CustomerOrder  |1----*|  OrderItem   |
  +------------+       +----------------+       +-------------+
  | id (PK)    |       | id (PK)        |       | id (PK)     |
  | name       |       | customerId(FK) |       | orderId(FK) |
  | email (UQ) |       | orderDate      |       | productId(FK)|
  | phone      |       | status         |       | quantity    |
  | address    |       | totalAmount    |       | unitPrice   |
  | city       |       | shippingAddress|       | lineTotal*  |
  | state      |       +----------------+       +-------------+
  | zipCode    |                                      |
  | createdAt  |                                      |
  +------------+                                      |
                                                      |
  +------------+       +----------------+             |
  |  Product   |1----1| InventoryItem  |             |
  +------------+       +----------------+             |
  | id (PK)    |       | id (PK)        |             |
  | name       |       | productId(FK,UQ)|            |
  | description|       | quantityOnHand |             |
  | category   |       | reorderLevel   |  *----------+
  | price      |       | warehouseLocation|
  | sku (UQ)   |       | lastRestocked  |
  | createdAt  |       +----------------+
  +------------+

  * lineTotal is a @Transient computed field (unitPrice x quantity)
```

### Table Naming

| Entity          | Table Name        | Notes                                             |
|-----------------|-------------------|----------------------------------------------------|
| `Customer`      | `customers`       |                                                    |
| `CustomerOrder` | `customer_orders` | Renamed from `Order` to avoid H2/SQL reserved word |
| `OrderItem`     | `order_items`     |                                                    |
| `Product`       | `products`        |                                                    |
| `InventoryItem` | `inventory_items` |                                                    |

### Key Constraints

- **Primary keys:** Auto-generated `IDENTITY` strategy (auto-increment)
- **Unique constraints:** `Customer.email`, `Product.sku`, `InventoryItem.productId`
- **Foreign keys:** Managed by JPA `@ManyToOne` / `@OneToMany` / `@OneToOne` annotations
- **Cascade:** `CascadeType.ALL` + `orphanRemoval = true` on parent-side collections

---

## REST API Reference

Base URL: `http://localhost:5001/api`

### Products API

| Method | Endpoint                        | Description              | Request Body | Response         |
|--------|---------------------------------|--------------------------|--------------|------------------|
| GET    | `/api/products`                 | List all products        | --           | `Product[]`      |
| GET    | `/api/products/{id}`            | Get product by ID        | --           | `Product`        |
| GET    | `/api/products/category/{name}` | Filter products by category | --        | `Product[]`      |
| POST   | `/api/products`                 | Create a new product     | `Product`    | `Product` (201)  |

#### GET /api/products -- Response Example

```json
[
  {
    "id": 1,
    "name": "Widget A",
    "description": "Standard widget",
    "category": "Widgets",
    "price": 9.99,
    "sku": "WGT-001",
    "createdAt": "2026-03-31T05:30:00",
    "inventory": {
      "id": 1,
      "productId": 1,
      "quantityOnHand": 50,
      "reorderLevel": 10,
      "warehouseLocation": "A-01",
      "lastRestocked": "2026-03-31T05:30:00"
    }
  }
]
```

#### POST /api/products -- Request Example

```json
{
  "name": "New Widget",
  "description": "A brand new widget",
  "category": "Widgets",
  "price": 24.99,
  "sku": "WGT-003"
}
```

---

### Customers API

| Method | Endpoint              | Description             | Request Body | Response          |
|--------|-----------------------|-------------------------|--------------|-------------------|
| GET    | `/api/customers`      | List all customers      | --           | `Customer[]`      |
| GET    | `/api/customers/{id}` | Get customer by ID      | --           | `Customer`        |
| POST   | `/api/customers`      | Create a new customer   | `Customer`   | `Customer` (201)  |

#### GET /api/customers -- Response Example

```json
[
  {
    "id": 1,
    "name": "Acme Corp",
    "email": "orders@acme.com",
    "phone": "555-0100",
    "address": "123 Main St",
    "city": "Springfield",
    "state": "IL",
    "zipCode": "62701",
    "createdAt": "2026-03-31T05:30:00",
    "orders": []
  }
]
```

---

### Orders API

| Method | Endpoint                     | Description                | Request Body           | Response              |
|--------|------------------------------|----------------------------|------------------------|-----------------------|
| GET    | `/api/orders`                | List all orders (newest first) | --                 | `CustomerOrder[]`     |
| GET    | `/api/orders/{id}`           | Get order by ID            | --                     | `CustomerOrder`       |
| POST   | `/api/orders`                | Create a new order         | `CreateOrderRequest`   | `CustomerOrder` (201) |
| PATCH  | `/api/orders/{id}/status`    | Update order status        | `UpdateStatusRequest`  | `CustomerOrder`       |

#### POST /api/orders -- Request Example

```json
{
  "customerId": 1,
  "items": [
    { "productId": 1, "quantity": 3 },
    { "productId": 3, "quantity": 1 }
  ]
}
```

#### POST /api/orders -- Response Example

```json
{
  "id": 1,
  "customerId": 1,
  "customer": {
    "id": 1,
    "name": "Acme Corp",
    "email": "orders@acme.com"
  },
  "orderDate": "2026-03-31T06:00:00",
  "status": "Pending",
  "totalAmount": 59.96,
  "shippingAddress": "123 Main St, Springfield, IL 62701",
  "items": [
    {
      "id": 1,
      "orderId": 1,
      "productId": 1,
      "product": { "id": 1, "name": "Widget A", "price": 9.99 },
      "quantity": 3,
      "unitPrice": 9.99,
      "lineTotal": 29.97
    },
    {
      "id": 2,
      "orderId": 1,
      "productId": 3,
      "product": { "id": 3, "name": "Gadget X", "price": 29.99 },
      "quantity": 1,
      "unitPrice": 29.99,
      "lineTotal": 29.99
    }
  ]
}
```

#### PATCH /api/orders/{id}/status -- Request Example

```json
{
  "status": "Shipped"
}
```

#### Order Creation Business Logic

1. Validates the customer exists
2. For each line item:
   - Validates the product exists
   - Validates the inventory record exists
   - Checks sufficient stock (`quantityOnHand >= requested quantity`)
   - Deducts the quantity from inventory
   - Creates an `OrderItem` with the current product price
3. Calculates the total amount (sum of `unitPrice * quantity` for all items)
4. Constructs the shipping address from the customer's address fields
5. Saves the order (cascades to order items)
6. All operations run within a single `@Transactional` scope -- any failure rolls back everything

---

### Inventory API

| Method | Endpoint                                | Description                        | Request Body      | Response          |
|--------|-----------------------------------------|------------------------------------|--------------------|-------------------|
| GET    | `/api/inventory`                        | List all inventory items           | --                 | `InventoryItem[]` |
| GET    | `/api/inventory/product/{productId}`    | Get inventory for a specific product | --               | `InventoryItem`   |
| POST   | `/api/inventory/product/{productId}/restock` | Restock a product             | `RestockRequest`   | `InventoryItem`   |
| GET    | `/api/inventory/low-stock`              | List items at or below reorder level | --               | `InventoryItem[]` |

#### POST /api/inventory/product/{productId}/restock -- Request Example

```json
{
  "quantity": 100
}
```

#### GET /api/inventory -- Response Example

```json
[
  {
    "id": 1,
    "productId": 1,
    "product": {
      "id": 1,
      "name": "Widget A",
      "description": "Standard widget",
      "category": "Widgets",
      "price": 9.99,
      "sku": "WGT-001",
      "createdAt": "2026-03-31T05:30:00"
    },
    "quantityOnHand": 50,
    "reorderLevel": 10,
    "warehouseLocation": "A-01",
    "lastRestocked": "2026-03-31T05:30:00"
  }
]
```

---

### Swagger UI & OpenAPI

| URL                              | Description                              |
|----------------------------------|------------------------------------------|
| `http://localhost:5001/swagger-ui.html` | Interactive Swagger UI                  |
| `http://localhost:5001/api-docs`        | OpenAPI 3.0 JSON specification          |

### H2 Console (Development)

| URL                              | Description                              |
|----------------------------------|------------------------------------------|
| `http://localhost:5001/h2-console` | H2 database web console               |
| JDBC URL                         | `jdbc:h2:file:./ordermanager`           |
| Username                         | `sa`                                     |
| Password                         | *(empty)*                                |

---

## Data Transfer Objects (DTOs)

### CreateOrderRequest

```java
public class CreateOrderRequest {
    private Integer customerId;            // Required: ID of the customer placing the order
    private List<OrderItemRequest> items;  // Optional: defaults to empty list if null
}
```

### OrderItemRequest

```java
public class OrderItemRequest {
    private Integer productId;   // Required: ID of the product to order
    private Integer quantity;    // Required: must be a positive integer
}
```

### RestockRequest

```java
public class RestockRequest {
    private Integer quantity;    // Required: must be a positive integer
}
```

### UpdateStatusRequest

```java
public class UpdateStatusRequest {
    private String status;       // New status value (e.g., "Pending", "Shipped", "Delivered")
}
```

---

## Configuration

### application.properties

```properties
# Application
spring.application.name=ordermanager-api
server.port=5001

# H2 Database (file-based, similar to SQLite)
spring.datasource.url=jdbc:h2:file:./ordermanager
spring.datasource.driver-class-name=org.h2.Driver
spring.datasource.username=sa
spring.datasource.password=

# JPA / Hibernate
spring.jpa.hibernate.ddl-auto=update       # Auto-creates/updates tables from entities
spring.jpa.show-sql=false                   # Set to true for debugging SQL
spring.jpa.open-in-view=false               # Disables OSIV to prevent lazy loading leaks

# H2 Console (for development)
spring.h2.console.enabled=true
spring.h2.console.path=/h2-console

# Jackson JSON
spring.jackson.serialization.write-dates-as-timestamps=false   # ISO 8601 date format

# OpenAPI / Swagger
springdoc.api-docs.path=/api-docs
springdoc.swagger-ui.path=/swagger-ui.html
```

### Key Configuration Decisions

| Setting                           | Value    | Rationale                                                  |
|-----------------------------------|----------|------------------------------------------------------------|
| `server.port`                     | `5001`   | Matches original .NET application port                     |
| `spring.jpa.open-in-view`         | `false`  | Prevents lazy loading outside transactions (explicit control) |
| `spring.jpa.hibernate.ddl-auto`   | `update` | Auto-manages schema (suitable for dev; use Flyway/Liquibase in production) |
| `write-dates-as-timestamps`       | `false`  | ISO 8601 strings for JSON dates (matches .NET JSON output) |

---

## Data Seeding

`DataSeeder` implements `CommandLineRunner` and populates the database on application startup:

- **3 Customers:** Acme Corp, Globex Inc, Initech LLC (with full address data)
- **5 Products:** Widget A, Widget B, Gadget X, Gadget Y, Thingamajig (various categories and prices)
- **5 Inventory Items:** One per product, with quantities from 50 to 250 and warehouse locations A-01 through A-05

**Guard clause:** Seeding only runs if the products table is empty (`productRepository.count() > 0` returns early), so it's safe across restarts with a persistent H2 file.

**Conditional activation:** Controlled by `app.seed-data.enabled` property (defaults to `true`). Set to `false` in test profiles to avoid polluting test data.

---

## Testing Strategy

### Test Configuration

Tests use the `test` profile (`@ActiveProfiles("test")`) which loads `application-test.properties`:

```properties
spring.datasource.url=jdbc:h2:mem:testdb;DB_CLOSE_DELAY=-1   # In-memory H2
spring.jpa.hibernate.ddl-auto=create-drop                     # Fresh schema per test run
app.seed-data.enabled=false                                    # No seed data
```

### Test Structure

```java
@SpringBootTest          // Full application context
@ActiveProfiles("test")  // Uses test config (in-memory DB, no seeder)
@Transactional           // Each test runs in a transaction that rolls back
class OrderServiceTest {
    @BeforeEach
    void setUp() {
        // Cleans all tables and inserts known test data
    }
}
```

### Test Cases

| Test                                           | Validates                                              |
|------------------------------------------------|--------------------------------------------------------|
| `getAllOrders_returnsEmptyList_whenNoOrders`    | Repository query returns empty list when no orders exist |
| `createOrder_deductsInventory`                 | Creating an order reduces `quantityOnHand` by the ordered amount |
| `createOrder_throwsOnInsufficientStock`        | Ordering more than available stock throws `IllegalStateException` |

### Running Tests

```bash
cd server
mvn test
```

---

## Running the Application

### Prerequisites

- Java 17+
- Maven 3.x
- Node.js 18+ and npm

### Step 1: Build the Angular Frontend

```bash
cd client-app
npm install
npx ng build
```

The build outputs to `server/src/main/resources/static/` (configured in `angular.json`).

### Step 2: Start the Spring Boot Server

```bash
cd server
mvn spring-boot:run
```

The application starts on `http://localhost:5001`.

### Step 3: Access the Application

| URL                                    | Description            |
|----------------------------------------|------------------------|
| `http://localhost:5001/`               | Angular SPA home page  |
| `http://localhost:5001/products`       | Products list          |
| `http://localhost:5001/customers`      | Customers list         |
| `http://localhost:5001/inventory`      | Inventory list         |
| `http://localhost:5001/orders`         | Orders list            |
| `http://localhost:5001/swagger-ui.html`| Swagger UI             |
| `http://localhost:5001/h2-console`     | H2 database console    |

---

## .NET to Java Migration Reference

This section documents key decisions made during the conversion from .NET 8 / C# / Entity Framework Core / SQLite to Java 17 / Spring Boot 3.2 / Spring Data JPA / H2.

### Framework Mapping

| .NET Concept                     | Java/Spring Equivalent                          |
|----------------------------------|-------------------------------------------------|
| ASP.NET Core Web API             | Spring Boot + Spring MVC (`@RestController`)     |
| Entity Framework Core            | Spring Data JPA (Hibernate)                      |
| `DbContext`                      | `JpaRepository<T, ID>` interfaces                |
| `DbSet<T>`                       | `JpaRepository.findAll()`, `save()`, etc.        |
| `.Include(x => x.Nav)`          | `FetchType.EAGER` on `@ManyToOne` / `@OneToMany` |
| `[JsonIgnore]`                   | `@JsonIgnore` / `@JsonIgnoreProperties`           |
| LINQ `.Where()`, `.OrderBy()`   | Spring Data derived queries / `@Query`            |
| SQLite                           | H2 (file-based mode)                              |
| `appsettings.json`              | `application.properties`                          |
| `Program.cs` / `Startup.cs`     | `@SpringBootApplication` + `@Configuration` beans |
| Swashbuckle (Swagger)           | SpringDoc OpenAPI                                  |
| xUnit                            | JUnit 5                                            |
| `DateTime.UtcNow`               | `LocalDateTime.now(ZoneOffset.UTC)`                |
| `decimal`                        | `BigDecimal`                                       |
| C# records / auto-properties    | Java POJOs with getters/setters (or Java records for DTOs) |

### Entity Naming

The `Order` entity was renamed to `CustomerOrder` because `ORDER` is a reserved keyword in H2 (and most SQL databases). The table is explicitly mapped to `customer_orders`.

### Relationship Strategy

The original .NET code used `[JsonIgnore]` / `ReferenceHandler.IgnoreCycles` to handle circular references. The Java version uses `@JsonIgnoreProperties` on each side of bidirectional relationships, which provides more granular control and ensures related data (like customer name on orders, product name on inventory) is included in JSON responses.

### Validation Approach

The original .NET code relied on C# non-nullable value types (e.g., `int`, `decimal`) to prevent null inputs. Java's `Integer` and `BigDecimal` are nullable, so explicit null checks were added in service methods:

```java
if (quantity == null || quantity <= 0) {
    throw new IllegalArgumentException("Quantity must be a positive number");
}
```

### Timestamp Handling

All timestamps use `LocalDateTime.now(ZoneOffset.UTC)` to match the original .NET `DateTime.UtcNow` behavior. Jackson is configured with `write-dates-as-timestamps=false` to serialize as ISO 8601 strings.
