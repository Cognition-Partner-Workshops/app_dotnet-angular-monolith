# Migration Journey: .NET to Java Spring Boot

## Overview
This document defines the migration path from the .NET (ASP.NET Core + EF Core) backend to a Java (Spring Boot + Spring Data JPA) backend. The Angular frontend (`client-app/`) remains unchanged throughout all waves.

## Architecture
- **Current**: `src/OrderManager.Api/` (.NET 8, EF Core, SQLite)
- **Target**: `server-java/` (Java 17, Spring Boot 3, Spring Data JPA, SQLite)
- **Frontend**: `client-app/` (Angular 17, unchanged)

## Migration Waves

### Wave 1: Java Backend Initialization ✅
**Status**: Complete
**Goal**: Scaffold the full Java Spring Boot project with all layers.
**Deliverables**:
- Maven project structure with all dependencies
- JPA entity models matching .NET models
- Spring Data JPA repositories
- Service layer with full business logic (including order creation with inventory deduction)
- REST controllers matching exact API contract (`/api/customers`, `/api/products`, `/api/orders`, `/api/inventory`)
- DTOs for request bodies
- Seed data matching .NET seed data
- CORS configuration
- Swagger/OpenAPI via SpringDoc
- SPA fallback for Angular routing
- Unit tests for OrderService

### Wave 2: API Parity Validation
**Status**: Not started
**Goal**: Verify the Java backend produces identical JSON responses to the .NET backend for all endpoints.
**Tasks**:
- Create integration tests (`@SpringBootTest` with `TestRestTemplate`) for every endpoint
- Compare JSON output field names, nesting, date formats, and null handling against .NET responses
- Verify `orderDate` serialization format matches (ISO 8601)
- Verify `BigDecimal` serialization matches .NET `decimal` (no trailing zeros issues)
- Verify nested object serialization (e.g., `order.customer.name`, `order.items[].product.name`)
- Test PATCH `/api/orders/{id}/status` returns updated order
- Test error responses (404 for missing resources, 400/500 for validation failures)
- Document any discrepancies and fix them

### Wave 3: Angular Build Integration
**Status**: Not started
**Goal**: Integrate Angular build into the Java project's build pipeline.
**Tasks**:
- Configure `frontend-maven-plugin` in `pom.xml` to run `npm install` and `ng build` during Maven build
- Output Angular build artifacts to `server-java/src/main/resources/static/`
- Verify SPA fallback works (deep links like `/orders`, `/products` resolve to `index.html`)
- Verify the app runs as a single JAR (`mvn package` → `java -jar target/order-manager.jar`)
- Update `client-app/` proxy config if needed for development mode

### Wave 4: Database Migration & Data Compatibility
**Status**: Not started
**Goal**: Ensure database compatibility and provide migration tooling.
**Tasks**:
- Add Flyway or Liquibase for schema versioning (replace `ddl-auto: update`)
- Create initial migration script matching the EF Core-generated schema
- Write a data migration script/tool that can import data from the .NET SQLite database into the Java SQLite database
- Verify foreign key constraints, indexes, and column types match
- Test with production-like data volumes

### Wave 5: Error Handling & Resilience
**Status**: Not started
**Goal**: Add production-grade error handling matching or exceeding .NET behavior.
**Tasks**:
- Add `@ControllerAdvice` global exception handler
- Map `IllegalArgumentException` → 400 Bad Request
- Map `EntityNotFoundException` → 404 Not Found
- Map `IllegalStateException` (insufficient stock) → 409 Conflict
- Add request validation with `@Valid` and Bean Validation annotations on DTOs
- Add logging (SLF4J) at service layer
- Add health check endpoint (`/actuator/health`) via Spring Boot Actuator

### Wave 6: Testing & Quality Assurance
**Status**: Not started
**Goal**: Achieve comprehensive test coverage.
**Tasks**:
- Controller integration tests with `@WebMvcTest` for all 4 controllers
- Service unit tests for CustomerService, ProductService, InventoryService
- End-to-end tests: start Java backend + serve Angular, run Cypress/Playwright tests against the full stack
- Load testing to compare performance with .NET backend
- Code quality: add Checkstyle, SpotBugs, or SonarQube configuration

### Wave 7: Deployment & Cutover
**Status**: Not started
**Goal**: Deploy the Java backend and retire the .NET backend.
**Tasks**:
- Create Dockerfile for the Java backend (multi-stage: build Angular + Maven build → runtime JRE image)
- Create `docker-compose.yml` with the Java backend service
- Update CI/CD pipeline to build and deploy the Java backend
- Run both backends in parallel (shadow mode) and compare responses
- Switch traffic to Java backend
- Archive/remove `src/OrderManager.Api/` and `tests/OrderManager.Api.Tests/`
- Update README.md with new build/run instructions

## API Contract Reference
The following endpoints must be preserved exactly:

| Method | Path | Request Body |
|--------|------|-------------|
| GET | /api/customers | - |
| GET | /api/customers/{id} | - |
| POST | /api/customers | Customer JSON |
| GET | /api/products | - |
| GET | /api/products/{id} | - |
| GET | /api/products/category/{category} | - |
| POST | /api/products | Product JSON |
| GET | /api/orders | - |
| GET | /api/orders/{id} | - |
| POST | /api/orders | {customerId, items: [{productId, quantity}]} |
| PATCH | /api/orders/{id}/status | {status} |
| GET | /api/inventory | - |
| GET | /api/inventory/product/{productId} | - |
| POST | /api/inventory/product/{productId}/restock | {quantity} |
| GET | /api/inventory/low-stock | - |
