var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri(builder.Configuration["ServiceUrls:CustomerService"] + "/health"), name: "customer-service")
    .AddUrlGroup(new Uri(builder.Configuration["ServiceUrls:ProductService"] + "/health"), name: "product-service")
    .AddUrlGroup(new Uri(builder.Configuration["ServiceUrls:InventoryService"] + "/health"), name: "inventory-service")
    .AddUrlGroup(new Uri(builder.Configuration["ServiceUrls:OrderService"] + "/health"), name: "order-service");

var app = builder.Build();

app.UseCors();
app.UseStaticFiles();
app.MapReverseProxy();
app.MapHealthChecks("/health");
app.MapFallbackToFile("index.html");
app.Run();

public partial class Program { }
