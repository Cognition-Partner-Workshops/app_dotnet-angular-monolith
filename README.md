# OrderManager Microservices

A Java Spring Boot 3 + Spring Cloud + Angular 17 microservices application decomposed from a monolith. Each domain (Products, Customers, Orders, Inventory) runs as an independent service with its own database, communicating via REST through a Spring Cloud Gateway.

## Architecture

```
                    ┌─────────────────┐
                    │  Angular UI     │
                    │  (port 4200)    │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │  API Gateway    │
                    │  (port 8080)    │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │  Eureka Server  │
                    │  (port 8761)    │
                    └────────┬────────┘
              ┌──────┬───────┼───────┬──────┐
              │      │       │       │      │
         ┌────▼──┐┌──▼───┐┌─▼────┐┌─▼─────┐│
         │Product││Custom││Order ││Invent- ││
         │Service││er Svc││Svc   ││ory Svc ││
         │ :8081 ││:8082 ││:8083 ││ :8084  ││
         └───────┘└──────┘└──────┘└────────┘│
```

### Services

| Service | Port | Description |
|---------|------|-------------|
| **Eureka Server** | 8761 | Service Discovery — all services register here |
| **API Gateway** | 8080 | Spring Cloud Gateway — routes `/api/*` to services |
| **Product Service** | 8081 | Product catalog, pricing, categories |
| **Customer Service** | 8082 | Customer profiles, addresses |
| **Order Service** | 8083 | Order creation, status tracking (calls Customer & Product services) |
| **Inventory Service** | 8084 | Stock levels, warehouse locations, restock (calls Product service) |
| **Angular UI** | 4200 | Frontend — proxies API calls to Gateway |

### Inter-Service Communication

- **Order Service** → calls **Customer Service** (to validate customer, build shipping address) and **Product Service** (to get product prices)
- **Inventory Service** → can call **Product Service** (to validate products)
- All inter-service calls use **Spring WebFlux WebClient** with **load-balanced** Eureka discovery

## Tech Stack

- **Backend**: Java 17, Spring Boot 3.2, Spring Cloud 2023.0.0
- **Service Discovery**: Spring Cloud Netflix Eureka
- **API Gateway**: Spring Cloud Gateway
- **Data**: Spring Data JPA, H2 Database (per-service)
- **Inter-Service Communication**: WebClient with load balancing
- **Frontend**: Angular 17, TypeScript
- **API Docs**: SpringDoc OpenAPI / Swagger UI (per-service)
- **Build**: Maven (multi-module)
- **Health Checks**: Spring Boot Actuator

## Getting Started

### Prerequisites
- Java 17+
- Maven 3.8+
- Node.js 18+
- Angular CLI (`npm install -g @angular/cli`)

### Build all services

```bash
# From the project root
mvn clean package -DskipTests
```

### Start the services (in order)

```bash
# 1. Start Eureka Server (wait for it to be ready)
cd eureka-server && mvn spring-boot:run &

# 2. Start API Gateway
cd api-gateway && mvn spring-boot:run &

# 3. Start domain services (can be started in parallel)
cd product-service && mvn spring-boot:run &
cd customer-service && mvn spring-boot:run &
cd order-service && mvn spring-boot:run &
cd inventory-service && mvn spring-boot:run &

# 4. Start Angular frontend
cd client-app && npm install && ng serve
```

### Access the application

| URL | Description |
|-----|-------------|
| `http://localhost:4200` | Angular UI |
| `http://localhost:8080` | API Gateway |
| `http://localhost:8761` | Eureka Dashboard |
| `http://localhost:8081/swagger-ui.html` | Product Service Swagger |
| `http://localhost:8082/swagger-ui.html` | Customer Service Swagger |
| `http://localhost:8083/swagger-ui.html` | Order Service Swagger |
| `http://localhost:8084/swagger-ui.html` | Inventory Service Swagger |

### Run tests

```bash
# Run all tests
mvn test

# Run tests for a specific service
cd product-service && mvn test
```

## API Routes (via Gateway)

All API calls go through the gateway at `http://localhost:8080`:

| Method | Path | Service | Description |
|--------|------|---------|-------------|
| GET | `/api/products` | product-service | List all products |
| GET | `/api/products/{id}` | product-service | Get product by ID |
| GET | `/api/products/category/{cat}` | product-service | Products by category |
| POST | `/api/products` | product-service | Create product |
| GET | `/api/customers` | customer-service | List all customers |
| GET | `/api/customers/{id}` | customer-service | Get customer by ID |
| POST | `/api/customers` | customer-service | Create customer |
| GET | `/api/orders` | order-service | List all orders |
| GET | `/api/orders/{id}` | order-service | Get order by ID |
| POST | `/api/orders` | order-service | Create order |
| PUT | `/api/orders/{id}/status` | order-service | Update order status |
| GET | `/api/inventory` | inventory-service | List all inventory |
| GET | `/api/inventory/product/{id}` | inventory-service | Get inventory by product |
| GET | `/api/inventory/low-stock` | inventory-service | Low stock items |
| POST | `/api/inventory/restock/{id}` | inventory-service | Restock product |

## Project Structure

```
.
├── pom.xml                      # Parent POM (multi-module Maven)
├── eureka-server/               # Service Discovery
│   ├── src/main/java/.../EurekaServerApplication.java
│   └── src/main/resources/application.properties
├── api-gateway/                 # Spring Cloud Gateway
│   ├── src/main/java/.../ApiGatewayApplication.java
│   ├── src/main/java/.../config/CorsConfig.java
│   └── src/main/resources/application.yml
├── product-service/             # Product domain microservice
│   ├── src/main/java/.../model/Product.java
│   ├── src/main/java/.../repository/ProductRepository.java
│   ├── src/main/java/.../service/ProductService.java
│   ├── src/main/java/.../controller/ProductsController.java
│   └── src/main/java/.../config/DataSeeder.java
├── customer-service/            # Customer domain microservice
│   ├── src/main/java/.../model/Customer.java
│   ├── src/main/java/.../repository/CustomerRepository.java
│   ├── src/main/java/.../service/CustomerService.java
│   ├── src/main/java/.../controller/CustomersController.java
│   └── src/main/java/.../config/DataSeeder.java
├── order-service/               # Order domain microservice
│   ├── src/main/java/.../model/CustomerOrder.java
│   ├── src/main/java/.../model/OrderItem.java
│   ├── src/main/java/.../repository/OrderRepository.java
│   ├── src/main/java/.../service/OrderService.java
│   ├── src/main/java/.../controller/OrdersController.java
│   ├── src/main/java/.../client/CustomerClient.java
│   └── src/main/java/.../client/ProductClient.java
├── inventory-service/           # Inventory domain microservice
│   ├── src/main/java/.../model/InventoryItem.java
│   ├── src/main/java/.../repository/InventoryRepository.java
│   ├── src/main/java/.../service/InventoryService.java
│   ├── src/main/java/.../controller/InventoryController.java
│   ├── src/main/java/.../client/ProductClient.java
│   └── src/main/java/.../config/DataSeeder.java
├── client-app/                  # Angular 17 frontend
│   ├── src/app/modules/         # Feature modules
│   ├── proxy.conf.json          # Dev proxy → API Gateway
│   └── angular.json
└── docs/
    └── API_AND_ARCHITECTURE.md  # Architecture documentation
```

## Cloud-Ready Configuration

Each microservice includes:
- **Eureka Client** registration for service discovery
- **Spring Boot Actuator** health endpoints (`/actuator/health`, `/actuator/info`)
- **Externalized configuration** via `application.properties`
- **Independent H2 database** (easily swappable to MySQL/PostgreSQL)
- **CORS configuration** on the API Gateway
- **Load-balanced WebClient** for inter-service communication
- **Swagger/OpenAPI docs** per service

## Decomposition Targets

This microservices architecture conforms to the
[platform-engineering-shared-services](https://github.com/Cognition-Partner-Workshops/platform-engineering-shared-services) standard.

See the companion IaC repo: [app_dotnet-angular-monolith-iac](https://github.com/Cognition-Partner-Workshops/app_dotnet-angular-monolith-iac)

## License

MIT
