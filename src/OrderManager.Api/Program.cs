// OrderManager API — Application entry point
// Configures services, middleware pipeline, and database seeding for the monolithic
// order management system backed by SQLite and an Angular SPA.

using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Data access ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=ordermanager.db"));

// --- Domain services (scoped per-request) ---
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<InventoryService>();

// --- MVC & serialization ---
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

// --- Swagger / OpenAPI ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- CORS (permissive for development) ---
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Ensure the database is created and populated with demo data on startup.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    SeedData.Initialize(context);
}

// --- Middleware pipeline ---
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseStaticFiles();
app.MapControllers();
app.MapFallbackToFile("index.html"); // Serves the Angular SPA for any unmatched routes.
app.Run();
