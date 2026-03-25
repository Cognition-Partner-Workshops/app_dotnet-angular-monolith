using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

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

    private static InventoryHttpClient CreateMockInventoryClient(
        bool checkStockResult = true,
        InventoryItemDto? deductResult = null)
    {
        var handler = new FakeInventoryHandler(checkStockResult, deductResult);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://fake-inventory") };
        return new InventoryHttpClient(httpClient);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryService()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var deductResult = new InventoryItemDto
        {
            Id = 1, ProductId = product.Id, ProductName = product.Name,
            QuantityOnHand = 45, ReorderLevel = 10, WarehouseLocation = "A-01"
        };
        var inventoryClient = CreateMockInventoryClient(checkStockResult: true, deductResult: deductResult);
        var service = new OrderService(context, inventoryClient);

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });
        Assert.NotNull(order);
        Assert.Single(order.Items);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var stockLevels = new Dictionary<int, int> { { product.Id, 2 } };
        var inventoryClient = CreateInventoryClient(stockLevels);
        var service = new OrderService(context, inventoryClient);

        var inventoryClient = CreateMockInventoryClient(checkStockResult: false, deductResult: null);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

internal class FakeInventoryHandler : HttpMessageHandler
{
    private readonly bool _checkStockResult;
    private readonly InventoryItemDto? _deductResult;

    public FakeInventoryHandler(bool checkStockResult, InventoryItemDto? deductResult)
    {
        _checkStockResult = checkStockResult;
        _deductResult = deductResult;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        if (path.Contains("/check"))
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = JsonContent.Create(new { productId = 1, quantity = 5, available = _checkStockResult });
            return Task.FromResult(response);
        }

        if (path.Contains("/deduct"))
        {
            if (_deductResult is null)
            {
                var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
                response.Content = JsonContent.Create(new { error = "Insufficient stock" });
                return Task.FromResult(response);
            }
            else
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = JsonContent.Create(_deductResult);
                return Task.FromResult(response);
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
