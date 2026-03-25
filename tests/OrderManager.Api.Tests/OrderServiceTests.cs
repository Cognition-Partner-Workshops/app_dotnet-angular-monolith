using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using OrderManager.Api.Data;
using OrderManager.Api.Services;

namespace OrderManager.Api.Tests;

/// <summary>
/// In-memory fake that implements IInventoryServiceClient for unit tests.
/// </summary>
public class FakeInventoryServiceClient : IInventoryServiceClient
{
    private readonly Dictionary<int, int> _stock = new();

    public MockInventoryServiceClient(int defaultStock = 50)
    {
        { 1, 50 }, { 2, 100 }, { 3, 150 }, { 4, 200 }, { 5, 250 }
    };

    public Task<List<InventoryDto>> GetAllInventoryAsync() =>
        Task.FromResult(_stock.Select(kv => new InventoryDto { ProductId = kv.Key, QuantityOnHand = kv.Value }).ToList());

    public Task<InventoryDto?> GetInventoryByProductIdAsync(int productId) =>
        Task.FromResult(_stock.ContainsKey(productId)
            ? new InventoryDto { ProductId = productId, QuantityOnHand = _stock[productId] }
            : null);

    public Task<InventoryDto> RestockAsync(int productId, int quantity)
    {
        if (_stock.ContainsKey(productId)) _stock[productId] += quantity;
        return Task.FromResult(new InventoryDto { ProductId = productId, QuantityOnHand = _stock.GetValueOrDefault(productId) });
    }

    public Task<List<InventoryItemDto>> GetAllInventoryAsync()
        => Task.FromResult(_stock.Select(kvp => new InventoryItemDto { ProductId = kvp.Key, QuantityOnHand = kvp.Value }).ToList());

    public Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId)
        => Task.FromResult(_stock.ContainsKey(productId) ? new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] } : null);

    public Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        _stock[productId] = _stock.GetValueOrDefault(productId) + quantity;
        return Task.FromResult(new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] });
    }

    public Task<List<InventoryItemDto>> GetLowStockItemsAsync()
        => Task.FromResult(_stock.Where(kvp => kvp.Value <= 10).Select(kvp => new InventoryItemDto { ProductId = kvp.Key, QuantityOnHand = kvp.Value }).ToList());

    public Task<InventoryItemDto?> DeductStockAsync(int productId, int quantity)
    {
        if (!_stock.ContainsKey(productId))
            throw new InvalidOperationException($"No inventory record for product {productId}");
        if (_stock[productId] < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");
        _stock[productId] -= quantity;
        return Task.FromResult(new InventoryDto { ProductId = productId, QuantityOnHand = _stock[productId] });
    }

    public Task<List<InventoryDto>> GetLowStockItemsAsync() =>
        Task.FromResult(new List<InventoryDto>());
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
        var inventoryService = CreateInventoryService();
        var service = new OrderService(context, inventoryService);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryService()
    {
        using var context = CreateContext();
        var inventoryService = CreateInventoryService(stockAvailable: true);
        var service = new OrderService(context, inventoryService);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var inventoryClient = CreateInventoryApiClient(HttpStatusCode.OK);
        var service = new OrderService(context, inventoryClient);

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Price * 5, order.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var inventoryService = CreateInventoryService(stockAvailable: false);
        var service = new OrderService(context, inventoryService);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var inventoryClient = CreateInventoryApiClient(HttpStatusCode.Conflict);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

/// <summary>
/// Fake HTTP handler that simulates inventory-service responses for unit tests.
/// </summary>
public class FakeInventoryHandler : HttpMessageHandler
{
    private readonly bool _stockAvailable;

    public FakeInventoryHandler(bool stockAvailable = true)
    {
        _stockAvailable = stockAvailable;
    }

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
