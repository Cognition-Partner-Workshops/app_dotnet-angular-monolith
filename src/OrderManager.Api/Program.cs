// =============================================================================
// OrderManager API — Application Entry Point
// Configures services, middleware, and the HTTP request pipeline for the
// monolithic .NET 8 + Angular 17 application.
// =============================================================================

using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Service Registration
// ---------------------------------------------------------------------------

// Register the EF Core database context with SQLite; falls back to a local file if no connection string is configured
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=ordermanager.db"));

// Register scoped business-logic services (one instance per HTTP request)
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<InventoryService>();

// Configure MVC controllers with JSON options to handle circular references between navigation properties
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

// Enable OpenAPI/Swagger documentation generation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Allow all origins for development; should be restricted in production
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// ---------------------------------------------------------------------------
// Database Initialization
// ---------------------------------------------------------------------------

// Seed the database with sample data on first run (no-ops if data already exists)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    SeedData.Initialize(context);
}

// ---------------------------------------------------------------------------
// Middleware Pipeline
// ---------------------------------------------------------------------------

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseStaticFiles();       // Serve the Angular build output from wwwroot/
app.MapControllers();        // Map attribute-routed API controllers
app.MapFallbackToFile("index.html"); // SPA fallback: serve index.html for unmatched routes
app.Run();
