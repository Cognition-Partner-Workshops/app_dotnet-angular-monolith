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

/// <summary>
/// In-memory mock of IInventoryServiceClient for testing.
/// Simulates inventory with a fixed stock of 50 per product.
/// </summary>
public class MockInventoryServiceClient : IInventoryServiceClient
{
    private readonly Dictionary<int, InventoryItem> _inventory;

    public MockInventoryServiceClient(int defaultStock = 50)
    {
        for (int i = 1; i <= 5; i++)
            _stock[i] = defaultStock;
    }

    public Task<List<InventoryItemDto>> GetAllInventoryAsync()
        => Task.FromResult(_stock.Select(kvp => new InventoryItemDto { ProductId = kvp.Key, QuantityOnHand = kvp.Value }).ToList());

    public Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId)
        => Task.FromResult(_stock.ContainsKey(productId) ? new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] } : null);

    public Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

    public Task<List<InventoryItemDto>> GetLowStockItemsAsync()
        => Task.FromResult(_stock.Where(kvp => kvp.Value <= 10).Select(kvp => new InventoryItemDto { ProductId = kvp.Key, QuantityOnHand = kvp.Value }).ToList());

    public Task<InventoryItemDto?> DeductStockAsync(int productId, int quantity)
    {
        if (!_stock.ContainsKey(productId))
            return Task.FromResult<InventoryItemDto?>(null);
        if (_stock[productId] < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");
        _stock[productId] -= quantity;
        return Task.FromResult<InventoryItemDto?>(new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] });
    }

    public Task<int> GetStockLevelAsync(int productId)
        => Task.FromResult(_stock.GetValueOrDefault(productId));
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

    private static InventoryHttpClient CreateInventoryClient(HttpMessageHandler? handler = null)
    {
        handler ??= new FakeInventoryHttpHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5100") };
        return new InventoryHttpClient(httpClient);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryService()
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.Single(order.Items);
        Assert.Equal(product.Price * 5, order.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryServiceClient(defaultStock: 2);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

internal class FakeInventoryHandler : HttpMessageHandler
{
    private readonly bool _stockAvailable;

    public FakeInventoryHandler(bool stockAvailable = true)
    {
        _stockAvailable = stockAvailable;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        if (path.Contains("check-stock"))
        {
            var json = JsonSerializer.Serialize(new { productId = 1, quantity = 1, available = _stockAvailable });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }

        if (path.Contains("deduct"))
        {
            if (!_stockAvailable)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = new StringContent("{\"error\":\"Insufficient stock\"}", System.Text.Encoding.UTF8, "application/json")
                });
            }

            var json = JsonSerializer.Serialize(new { id = 1, productId = 1, productName = "Widget A", quantityOnHand = 45, reorderLevel = 10, warehouseLocation = "A-01", lastRestocked = DateTime.UtcNow });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
        });
    }
}
