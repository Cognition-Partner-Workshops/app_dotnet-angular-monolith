# OrderManager - Microservice Decomposition

A .NET 8 + Angular 17 application demonstrating monolith-to-microservices decomposition. The **Inventory** module has been extracted into a standalone microservice, while Orders, Products, and Customers remain in the main API.

## Architecture

```
                    +---------------------+
                    |   Angular 17 SPA    |
                    +---------------------+
                       |              |
                       v              v
              +-----------------+  +---------------------+
              | OrderManager.Api|  | InventoryService.Api |
              | (Port 5001)     |  | (Port 5002)          |
              +-----------------+  +---------------------+
              | Orders          |  | Inventory Items      |
              | Products        |  | Stock Levels         |
              | Customers       |  | Warehouse Locations  |
              +-----------------+  +---------------------+
              | SQLite:         |  | SQLite:              |
              | ordermanager.db |  | inventory.db         |
              +-----------------+  +---------------------+
```

### Service Communication

- **OrderManager.Api** calls **InventoryService.Api** via HTTP to check/deduct stock during order creation
- The monolith's `/api/inventory` endpoint proxies requests to the Inventory microservice via `InventoryApiClient`
- Each service has its own independent SQLite database

### Modules

| Service | Module | Description |
|---------|--------|-------------|
| **OrderManager.Api** | Orders | Order creation, status tracking, fulfillment |
| **OrderManager.Api** | Products | Product catalog, pricing, categories |
| **OrderManager.Api** | Customers | Customer profiles, addresses, preferences |
| **InventoryService.Api** | Inventory | Stock levels, warehouse locations, reorder, stock check/deduction |

## Tech Stack

- **Backend**: .NET 8, C#, Entity Framework Core, SQLite
- **Frontend**: Angular 17, TypeScript
- **API**: RESTful with Swagger/OpenAPI
- **Communication**: HTTP/REST between services

## Getting Started

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- Angular CLI (`npm install -g @angular/cli`)

### Run the application

```bash
# Restore all .NET dependencies
dotnet restore OrderManager.sln

# Install Angular dependencies
cd client-app && npm install && cd ..

# Start the Inventory microservice (terminal 1)
dotnet run --project src/InventoryService.Api/InventoryService.Api.csproj

# Start the OrderManager API (terminal 2)
dotnet run --project src/OrderManager.Api/OrderManager.Api.csproj
```

- **OrderManager API**: `https://localhost:5001` (also serves the Angular app)
- **Inventory Service**: `http://localhost:5002` (API + Swagger at `/swagger`)

### Run tests

```bash
dotnet test OrderManager.sln
```

## Inventory Service API

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/inventory` | List all inventory items |
| GET | `/api/inventory/product/{productId}` | Get inventory for a product |
| POST | `/api/inventory/product/{productId}/restock` | Restock a product |
| GET | `/api/inventory/low-stock` | Get items below reorder level |
| POST | `/api/inventory/product/{productId}/check-and-deduct` | Check and deduct stock |
| GET | `/api/inventory/product/{productId}/stock` | Get stock level |

## Configuration

The OrderManager API connects to the Inventory microservice via the `InventoryService:BaseUrl` setting in `appsettings.json`:

```json
{
  "InventoryService": {
    "BaseUrl": "http://localhost:5002"
  }
}
```

## Decomposition Targets

This repo demonstrates decomposition from monolith to microservices, conforming to the
[platform-engineering-shared-services](https://github.com/Cognition-Partner-Workshops/platform-engineering-shared-services) standard.

See the companion IaC repo: [app_dotnet-angular-monolith-iac](https://github.com/Cognition-Partner-Workshops/app_dotnet-angular-monolith-iac)

## License

MIT
