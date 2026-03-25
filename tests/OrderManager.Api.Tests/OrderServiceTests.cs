using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;

namespace OrderManager.Api.Tests;

public class FakeInventoryServiceClient : IInventoryServiceClient
{
    private readonly Dictionary<int, int> _stock = new()
    {
        { 1, 50 }, { 2, 100 }, { 3, 150 }, { 4, 200 }, { 5, 250 }
    };

    public Task<List<InventoryCheckResult>> GetAllInventoryAsync() =>
        Task.FromResult(_stock.Select(kv => new InventoryCheckResult { ProductId = kv.Key, QuantityOnHand = kv.Value }).ToList());

    public Task<InventoryCheckResult?> GetInventoryByProductIdAsync(int productId) =>
        Task.FromResult(_stock.ContainsKey(productId) ? new InventoryCheckResult { ProductId = productId, QuantityOnHand = _stock[productId] } : null);

    public Task<List<InventoryCheckResult>> GetLowStockItemsAsync() =>
        Task.FromResult(new List<InventoryCheckResult>());

    public Task<InventoryCheckResult?> RestockAsync(int productId, int quantity)
    {
        if (_stock.ContainsKey(productId)) _stock[productId] += quantity;
        return Task.FromResult<InventoryCheckResult?>(new InventoryCheckResult { ProductId = productId, QuantityOnHand = _stock.GetValueOrDefault(productId) });
    }

    public Task<bool> CheckStockAsync(int productId, int quantity) =>
        Task.FromResult(_stock.ContainsKey(productId) && _stock[productId] >= quantity);

    public Task<InventoryCheckResult?> DeductStockAsync(int productId, int quantity)
    {
        if (!_stock.ContainsKey(productId))
            throw new InvalidOperationException($"No inventory record for product {productId}");
        if (_stock[productId] < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");
        _stock[productId] -= quantity;
        return Task.FromResult<InventoryCheckResult?>(new InventoryCheckResult { ProductId = productId, QuantityOnHand = _stock[productId] });
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

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = new FakeInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_SucceedsWhenStockAvailable()
    {
        using var context = CreateContext();
        var inventoryClient = new FakeInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var stockLevels = new Dictionary<int, int> { { product.Id, 100 } };
        var inventoryClient = CreateInventoryClient(stockLevels);
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
        var inventoryClient = new FakeInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var stockLevels = new Dictionary<int, int> { { product.Id, 2 } };
        var inventoryClient = CreateInventoryClient(stockLevels);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

public class FakeInventoryHandler : HttpMessageHandler
{
    private readonly bool _stockAvailable;
    private readonly bool _deductSucceeds;

    public FakeInventoryHandler(bool stockAvailable = true, bool deductSucceeds = true)
    {
        _stockAvailable = stockAvailable;
        _deductSucceeds = deductSucceeds;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        if (path.Contains("/check"))
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { productId = 1, quantity = 5, available = _stockAvailable })
            };
            return Task.FromResult(response);
        }

        if (path.Contains("/deduct"))
        {
            if (_deductSucceeds)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new InventoryItemDto
                    {
                        Id = 1, ProductId = 1, ProductName = "Widget A",
                        QuantityOnHand = 45, ReorderLevel = 10,
                        WarehouseLocation = "A-01", LastRestocked = DateTime.UtcNow
                    })
                };
                return Task.FromResult(response);
            }
            else
            {
                var response = new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = JsonContent.Create(new { error = "Insufficient stock" })
                };
                return Task.FromResult(response);
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
