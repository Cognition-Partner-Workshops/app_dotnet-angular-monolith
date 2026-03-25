using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using OrderManager.Api.Data;
using OrderManager.Api.Services;

namespace OrderManager.Api.Tests;

/// <summary>
/// Fake HTTP handler that simulates inventory-service responses for testing.
/// </summary>
public class FakeInventoryHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<int, int> _stock = new()
    {
        { 1, 50 }, { 2, 100 }, { 3, 150 }, { 4, 200 }, { 5, 250 }
    };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        if (path.Contains("/deduct"))
        {
            var segments = path.Split('/');
            var productIdStr = segments[^2];
            if (int.TryParse(productIdStr, out var pid) && _stock.TryGetValue(pid, out var currentQty))
            {
                var body = await request.Content!.ReadAsStringAsync(cancellationToken);
                var doc = JsonDocument.Parse(body);
                // Handle both PascalCase and camelCase property names
                int qty;
                if (doc.RootElement.TryGetProperty("Quantity", out var qPascal))
                    qty = qPascal.GetInt32();
                else if (doc.RootElement.TryGetProperty("quantity", out var qCamel))
                    qty = qCamel.GetInt32();
                else
                    throw new InvalidOperationException("No quantity property found in request body");

                if (currentQty < qty)
                {
                    return new HttpResponseMessage(HttpStatusCode.Conflict)
                    {
                        Content = new StringContent($"{{\"error\":\"Insufficient stock\"}}", System.Text.Encoding.UTF8, "application/json")
                    };
                }
                _stock[pid] = currentQty - qty;
                var item = new InventoryItemDto { Id = pid, ProductId = pid, QuantityOnHand = _stock[pid] };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(item), System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
        };
    }
}

public class OrderServiceTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        SeedData.Initialize(context);
        return context;
    }

    private static InventoryServiceClient CreateInventoryClient(HttpMessageHandler? handler = null)
    {
        handler ??= new FakeInventoryHttpHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5002") };
        return new InventoryServiceClient(httpClient, NullLogger<InventoryServiceClient>.Instance);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var service = new OrderService(context, CreateInventoryClient());
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_DeductsStockViaMicroservice()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var service = new OrderService(context, CreateInventoryClient());

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(5, order.Items.First().Quantity);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var service = new OrderService(context, CreateInventoryClient());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
