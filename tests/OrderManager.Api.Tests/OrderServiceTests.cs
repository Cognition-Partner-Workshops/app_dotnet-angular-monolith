using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

/// <summary>
/// A fake HTTP message handler for testing the InventoryHttpClient
/// </summary>
public class FakeInventoryHandler : HttpMessageHandler
{
    private readonly Dictionary<int, InventoryItemDto> _inventory = new();
    private int _nextId = 1;

    public void AddItem(int productId, string productName, int quantity)
    {
        _inventory[productId] = new InventoryItemDto
        {
            Id = _nextId++,
            ProductId = productId,
            ProductName = productName,
            QuantityOnHand = quantity,
            ReorderLevel = 10,
            WarehouseLocation = "A-01",
            LastRestocked = DateTime.UtcNow
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        // POST /api/inventory/product/{id}/deduct
        if (request.Method == HttpMethod.Post && path.Contains("/deduct"))
        {
            var segments = path.Split('/');
            var productIdStr = segments[^2]; // product id is before "deduct"
            if (int.TryParse(productIdStr, out var productId) && _inventory.TryGetValue(productId, out var item))
            {
                var body = request.Content?.ReadFromJsonAsync<DeductRequest>(cancellationToken: cancellationToken).Result;
                if (body is not null && item.QuantityOnHand >= body.Quantity)
                {
                    item.QuantityOnHand -= body.Quantity;
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(item)
                    });
                }
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = JsonContent.Create(new { error = $"Insufficient stock for product {productId}. Available: {item.QuantityOnHand}" })
                });
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private record DeductRequest(int Quantity);
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

    private (OrderService service, FakeInventoryHandler handler) CreateServiceWithFakeInventory(AppDbContext context)
    {
        var handler = new FakeInventoryHandler();
        // Seed fake inventory to match seeded products
        var products = context.Products.ToList();
        for (int i = 0; i < products.Count; i++)
        {
            handler.AddItem(products[i].Id, products[i].Name, (i + 1) * 50);
        }
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://fake-inventory") };
        var inventoryClient = new InventoryHttpClient(httpClient);
        var service = new OrderService(context, inventoryClient);
        return (service, handler);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var (service, _) = CreateServiceWithFakeInventory(context);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_DeductsInventoryViaHttpClient()
    {
        using var context = CreateContext();
        var (service, _) = CreateServiceWithFakeInventory(context);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Id, order.Items.First().ProductId);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var (service, _) = CreateServiceWithFakeInventory(context);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
