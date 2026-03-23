// =============================================================================
// OrderManager API — Application Entry Point
// =============================================================================
// This is the main entry point for the OrderManager monolith backend.
// It configures and starts the ASP.NET Core web application, wiring up:
//   - Entity Framework Core with SQLite for data persistence
//   - Scoped business logic services (Orders, Products, Customers, Inventory)
//   - MVC controllers with JSON serialization settings
//   - Swagger/OpenAPI for API documentation
//   - CORS policy for cross-origin frontend requests
//   - Static file serving for the Angular SPA
// =============================================================================

using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Entity Framework Core with SQLite provider.
// Falls back to a local "ordermanager.db" file if no connection string is configured.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=ordermanager.db"));

// Register business logic services with scoped lifetime (one instance per HTTP request)
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<InventoryService>();

// Configure MVC controllers with JSON options to handle circular references
// between related entities (e.g., Order -> Customer -> Orders)
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

// Enable Swagger/OpenAPI endpoint discovery and documentation generation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure a permissive CORS policy for local development with the Angular frontend
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Seed the database with sample data on application startup.
// Uses a scoped service provider to resolve the DbContext outside the request pipeline.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    SeedData.Initialize(context);
}

// Enable Swagger UI for interactive API testing (available at /swagger)
app.UseSwagger();
app.UseSwaggerUI();

// Enable CORS, static file serving (for Angular SPA assets), and controller routing
app.UseCors();
app.UseStaticFiles();
app.MapControllers();

// Fallback to index.html for Angular client-side routing support.
// Any request that doesn't match an API route or static file will serve the SPA shell.
app.MapFallbackToFile("index.html");
app.Run();
