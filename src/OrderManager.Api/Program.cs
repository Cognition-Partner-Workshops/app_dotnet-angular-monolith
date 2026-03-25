using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Services;

// ---------------------------------------------------------------------------
// Application bootstrap — configures DI, middleware, and the request pipeline.
// This is the single entry point for the OrderManager monolith, which hosts
// both the ASP.NET Core API and the Angular SPA (served as static files).
// ---------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

// --- Persistence -----------------------------------------------------------
// Uses SQLite for local development; connection string is in appsettings.json.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=ordermanager.db"));

// --- Business services (scoped per-request) --------------------------------
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<InventoryService>();

// --- API / Swagger ---------------------------------------------------------
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- CORS (permissive for local dev — tighten for production) --------------
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// --- Database seeding — runs once on startup to populate demo data ---------
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    SeedData.Initialize(context);
}

// --- Middleware pipeline ---------------------------------------------------
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseStaticFiles();       // Serves the compiled Angular SPA from wwwroot
app.MapControllers();
app.MapFallbackToFile("index.html"); // SPA fallback for client-side routing
app.Run();
