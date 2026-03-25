using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Clients;
using OrderManager.Api.Data;
using OrderManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=ordermanager.db"));

builder.Services.AddHttpClient<IInventoryClient, InventoryHttpClient>(client =>
{
    var baseUrl = builder.Configuration["InventoryService:BaseUrl"] ?? "http://localhost:5002";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CustomerService>();

// Register inventory service HTTP client pointing to the inventory microservice
var inventoryServiceUrl = builder.Configuration["InventoryServiceUrl"];
if (string.IsNullOrEmpty(inventoryServiceUrl))
    inventoryServiceUrl = "http://localhost:5001";

builder.Services.AddHttpClient<IInventoryServiceClient, InventoryServiceHttpClient>(client =>
{
    client.BaseAddress = new Uri(inventoryServiceUrl);
});

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    SeedData.Initialize(context);
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseStaticFiles();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapFallbackToFile("index.html");
app.Run();
