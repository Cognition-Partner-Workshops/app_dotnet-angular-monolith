using Microsoft.EntityFrameworkCore;
using OrderService.Api.Clients;
using OrderService.Api.Data;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=order.db"));

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt)));

builder.Services.AddHttpClient<ICustomerApiClient, CustomerApiClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:CustomerService"] ?? "http://localhost:5010"))
    .AddPolicyHandler(retryPolicy);

builder.Services.AddHttpClient<IProductApiClient, ProductApiClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:ProductService"] ?? "http://localhost:5020"))
    .AddPolicyHandler(retryPolicy);

builder.Services.AddHttpClient<IInventoryApiClient, InventoryApiClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:InventoryService"] ?? "http://localhost:5030"));

builder.Services.AddScoped<OrderService.Api.Services.OrderService>();

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
    var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    context.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();

public partial class Program { }
