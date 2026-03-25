using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=ordermanager.db"));

var inventoryServiceUrl = builder.Configuration["ServiceUrls:InventoryService"] ?? "http://localhost:5100";
builder.Services.AddHttpClient<InventoryHttpClient>(client =>
{
    client.BaseAddress = new Uri(inventoryServiceUrl);
});

builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CustomerService>();

// Inventory service: use HTTP client when InventoryServiceUrl is configured, otherwise fall back to local DB
var inventoryServiceUrl = builder.Configuration["InventoryServiceUrl"];
if (!string.IsNullOrEmpty(inventoryServiceUrl))
{
    builder.Services.AddHttpClient<IInventoryServiceClient, InventoryServiceHttpClient>(client =>
    {
        client.BaseAddress = new Uri(inventoryServiceUrl);
    });
}
else
{
    builder.Services.AddScoped<IInventoryServiceClient, InventoryService>();
}

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
