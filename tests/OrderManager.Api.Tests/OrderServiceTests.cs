using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

/// <summary>
/// A test HTTP message handler that simulates inventory-service responses.
/// </summary>
public class FakeInventoryHandler : HttpMessageHandler
{
    private readonly Dictionary<int, int> _stock = new()
    {
        { 1, 50 }, { 2, 100 }, { 3, 150 }, { 4, 200 }, { 5, 250 }
    };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";

        if (path.Contains("/deduct"))
        {
            var segments = path.Split('/');
            var productIdStr = segments[^2]; // product/{id}/deduct
            if (int.TryParse(productIdStr, out var productId) && _stock.ContainsKey(productId))
            {
                var body = await request.Content!.ReadFromJsonAsync<DeductBody>(cancellationToken: cancellationToken);
                if (body is not null && _stock[productId] >= body.Quantity)
                {
                    _stock[productId] -= body.Quantity;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(new InventoryItemDto
                        {
                            Id = productId, ProductId = productId, ProductName = $"Product {productId}",
                            QuantityOnHand = _stock[productId], ReorderLevel = 10, WarehouseLocation = $"A-{productId:D2}"
                        })
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = JsonContent.Create(new { error = $"Insufficient stock for product {productId}" })
                };
            }
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private record DeductBody(int Quantity);
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

    private static InventoryApiClient CreateInventoryClient(HttpMessageHandler? handler = null)
    {
        handler ??= new FakeInventoryHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5100") };
        return new InventoryApiClient(httpClient);
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
    public async Task CreateOrder_CallsInventoryService()
    {
        using var context = CreateContext();
        var service = new OrderService(context, CreateInventoryClient());
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(5, order.Items.First().Quantity);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var service = new OrderService(context, CreateInventoryClient());
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
