# Customers Microservice

This is the extracted Customers microservice, decomposed from the OrderManager monolith. It owns all Customer data and exposes a REST API for customer management.

## Prerequisites

- .NET 8 SDK

## Build & Run

```bash
cd CustomerService.Api
dotnet restore
dotnet build
dotnet run
```

The service runs on **http://localhost:5100** by default.

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/customers` | List all customers |
| GET | `/api/customers/{id}` | Get a customer by ID |
| POST | `/api/customers` | Create a new customer |
| PUT | `/api/customers/{id}` | Update an existing customer |
| DELETE | `/api/customers/{id}` | Delete a customer |

Swagger UI is available at **http://localhost:5100/swagger** when running in Development mode.

## Database

This microservice uses its own **SQLite** database (`customers.db`), completely independent of the monolith's database. On first startup, the database is automatically created and seeded with sample customer data matching the monolith's seed data.

## Architecture Notes

- This service was extracted from the `OrderManager` monolith's Customers domain.
- The `Customer` entity does **not** include an `Orders` navigation property — that relationship belongs to the Orders domain.
- The monolith's Customer code has **not** been removed. Future work will have the Orders service call this microservice via HTTP instead of direct DB access.
