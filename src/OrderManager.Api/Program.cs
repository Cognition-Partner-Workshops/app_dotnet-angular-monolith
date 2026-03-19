using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Services;

// ──────────────────────────────────────────────────────────────
// OrderManager Monolith – Application bootstrap
// This is a single-process host that serves both the .NET API
// and the Angular SPA (built into wwwroot via angular.json).
// ──────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// --- Data access: SQLite via Entity Framework Core ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=ordermanager.db"));

// --- Business-logic services (one per domain) ---
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<InventoryService>();

// --- MVC + JSON: ignore circular references caused by EF navigation properties ---
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

// --- Swagger / OpenAPI ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- CORS: permissive policy for local development ---
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// --- Seed the database with sample data on first run ---
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    SeedData.Initialize(context);
}

// --- Middleware pipeline ---
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseStaticFiles();       // Serve Angular build artifacts from wwwroot
app.MapControllers();       // Map attribute-routed API controllers
app.MapFallbackToFile("index.html"); // SPA fallback for Angular client-side routing
app.Run();
