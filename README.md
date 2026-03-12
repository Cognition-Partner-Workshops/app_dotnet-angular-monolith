# OrderManager Monolith

A .NET 8 + Angular 17 monolith application demonstrating tightly coupled modules sharing a single database. This is the **"before"** state for monolith-to-microservices decomposition demos.

## Architecture

The application contains four tightly coupled modules:

| Module | Description |
|--------|-------------|
| **Orders** | Order creation, status tracking, fulfillment |
| **Products** | Product catalog, pricing, categories |
| **Customers** | Customer profiles, addresses, preferences |
| **Inventory** | Stock levels, warehouse locations, reorder |

All modules share a single SQLite database and are deployed as one unit.

## Tech Stack

- **Backend**: .NET 8, C#, Entity Framework Core, SQLite
- **Frontend**: Angular 17, TypeScript
- **API**: RESTful with Swagger/OpenAPI

## Getting Started

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- Angular CLI (`npm install -g @angular/cli`)

### Run the application

```bash
# Restore .NET dependencies
dotnet restore src/OrderManager.Api/OrderManager.Api.csproj

# Install Angular dependencies
cd client-app && npm install && cd ..

# Run the API (serves Angular app too)
dotnet run --project src/OrderManager.Api/OrderManager.Api.csproj
```

The application will be available at `https://localhost:5001`.

## Decomposition Targets

This monolith is designed to be decomposed into microservices that conform to the
[platform-engineering-shared-services](https://github.com/Cognition-Partner-Workshops/platform-engineering-shared-services) standard.

See the companion IaC repo: [app_dotnet-angular-monolith-iac](https://github.com/Cognition-Partner-Workshops/app_dotnet-angular-monolith-iac)

## License

MIT
