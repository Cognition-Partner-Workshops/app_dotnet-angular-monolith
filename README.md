# OrderManager Monolith

A Java Spring Boot 3 + Angular 17 monolith application demonstrating tightly coupled modules sharing a single database. This is the **"before"** state for monolith-to-microservices decomposition demos.

## Architecture

The application contains four tightly coupled modules:

| Module | Description |
|--------|-------------|
| **Orders** | Order creation, status tracking, fulfillment |
| **Products** | Product catalog, pricing, categories |
| **Customers** | Customer profiles, addresses, preferences |
| **Inventory** | Stock levels, warehouse locations, reorder |

All modules share a single H2 database and are deployed as one unit.

## Tech Stack

- **Backend**: Java 17, Spring Boot 3.2, Spring Data JPA, H2 Database
- **Frontend**: Angular 17, TypeScript
- **API**: RESTful with OpenAPI/Swagger (SpringDoc)
- **Build**: Maven

## Getting Started

### Prerequisites
- Java 17+
- Maven 3.8+
- Node.js 18+
- Angular CLI (`npm install -g @angular/cli`)

### Run the application

```bash
# Build the Angular frontend
cd client-app && npm install && ng build && cd ..

# Run the Spring Boot API (serves Angular app too)
cd server && mvn spring-boot:run
```

The application will be available at `http://localhost:5001`.

### Run tests

```bash
cd server && mvn test
```

### API Documentation

Swagger UI is available at `http://localhost:5001/swagger-ui.html` when the application is running.

## Project Structure

```
.
├── client-app/              # Angular 17 frontend
│   ├── src/
│   │   ├── app/
│   │   │   ├── modules/     # Feature modules (orders, products, customers, inventory)
│   │   │   ├── app.component.ts
│   │   │   └── app.routes.ts
│   │   ├── environments/
│   │   ├── index.html
│   │   └── main.ts
│   ├── angular.json
│   ├── package.json
│   └── tsconfig.json
├── server/                  # Spring Boot backend
│   ├── src/
│   │   ├── main/
│   │   │   ├── java/com/ordermanager/api/
│   │   │   │   ├── config/       # CORS, Web, DataSeeder
│   │   │   │   ├── controller/   # REST controllers
│   │   │   │   ├── dto/          # Request DTOs
│   │   │   │   ├── model/        # JPA entities
│   │   │   │   ├── repository/   # Spring Data repositories
│   │   │   │   ├── service/      # Business logic
│   │   │   │   └── OrderManagerApplication.java
│   │   │   └── resources/
│   │   │       └── application.properties
│   │   └── test/
│   │       └── java/com/ordermanager/api/
│   │           └── service/      # Service tests
│   └── pom.xml
└── README.md
```

## Java Spring Boot Backend

A Java Spring Boot backend is available at `server-java/` as part of an ongoing migration from .NET to Java. It mirrors the same API contract as the .NET backend. See [`MIGRATION_PLAN.md`](MIGRATION_PLAN.md) for the full migration journey and wave definitions.

### Run the Java backend

```bash
cd server-java
./mvnw spring-boot:run
```

The Java backend will be available at `http://localhost:5000`.

## Decomposition Targets

This monolith is designed to be decomposed into microservices that conform to the
[platform-engineering-shared-services](https://github.com/Cognition-Partner-Workshops/platform-engineering-shared-services) standard.

See the companion IaC repo: [app_dotnet-angular-monolith-iac](https://github.com/Cognition-Partner-Workshops/app_dotnet-angular-monolith-iac)

## License

MIT
