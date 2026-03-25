using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Clients;
using OrderManager.Api.Data;
using OrderManager.Api.Services;
using Xunit;
using OrderManager.Api.Data;
using OrderManager.Api.Services;

namespace OrderManager.Api.Tests;

/// <summary>
/// In-memory mock of IInventoryClient for unit testing.
/// Simulates inventory stock levels without HTTP calls.
/// </summary>
public class MockInventoryClient : IInventoryClient
{
    private readonly Dictionary<int, int> _stock = new();

    public MockInventoryClient(int defaultStock = 50)
    {
        for (int i = 1; i <= 5; i++)
            _stock[i] = defaultStock;
    }

    public MockInventoryClient(Dictionary<int, int> initialStock)
    {
        foreach (var kvp in initialStock)
            _stock[kvp.Key] = kvp.Value;
    }

    public Task<List<InventoryItemDto>> GetAllInventoryAsync()
        => Task.FromResult(_stock.Select(kvp => new InventoryItemDto { ProductId = kvp.Key, QuantityOnHand = kvp.Value }).ToList());

    public Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId)
        => Task.FromResult(_stock.ContainsKey(productId)
            ? new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] }
            : (InventoryItemDto?)null);

    public Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        _stock[productId] = _stock.GetValueOrDefault(productId) + quantity;
        return Task.FromResult(new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] });
    }

    public Task<List<InventoryItemDto>> GetLowStockItemsAsync()
        => Task.FromResult(_stock.Where(kvp => kvp.Value <= 10)
            .Select(kvp => new InventoryItemDto { ProductId = kvp.Key, QuantityOnHand = kvp.Value }).ToList());

    public Task<bool> CheckStockAsync(int productId, int quantity)
        => Task.FromResult(_stock.ContainsKey(productId) && _stock[productId] >= quantity);

    public Task<InventoryItemDto> DeductStockAsync(int productId, int quantity)
    {
        if (!_stock.ContainsKey(productId) || _stock[productId] < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");
        _stock[productId] -= quantity;
        return Task.FromResult(new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] });
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

    private static InventoryService CreateInventoryService(bool stockAvailable = true)
    {
        var handler = new FakeInventoryHandler(stockAvailable);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://fake-inventory") };
        return new InventoryService(httpClient);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_DeductsStockViaInventoryService()
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var inventoryClient = new FakeInventoryClient(new Dictionary<int, int> { { product.Id, 100 } });
        var service = new OrderService(context, inventoryClient);

        var inventoryClient = new FakeInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(5, order.Items.First().Quantity);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryClient(defaultStock: 2);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var inventoryClient = new FakeInventoryClient(new Dictionary<int, int> { { product.Id, 2 } });
        var service = new OrderService(context, inventoryClient);

        var inventoryClient = new FakeInventoryServiceClient(shouldFail: true);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

public class FakeInventoryHandler : HttpMessageHandler
{
    private readonly bool _stockAvailable;

    public FakeInventoryHandler(bool stockAvailable = true)
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryClient(new Dictionary<int, int>());
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        if (path.Contains("/check"))
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { productId = 1, quantity = 1, available = _stockAvailable })
            };
            return Task.FromResult(response);
        }

        if (path.Contains("/deduct"))
        {
            if (!_stockAvailable)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict));

            var dto = new InventoryItemDto
            {
                Id = 1, ProductId = 1, ProductName = "Widget A",
                QuantityOnHand = 45, ReorderLevel = 10,
                WarehouseLocation = "A-01", LastRestocked = DateTime.UtcNow
            };
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(dto)
            };
            return Task.FromResult(response);
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
