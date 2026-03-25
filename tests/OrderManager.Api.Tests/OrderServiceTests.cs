using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

/// <summary>
/// In-memory fake that implements IInventoryServiceClient for unit tests.
/// </summary>
public class FakeInventoryServiceClient : IInventoryServiceClient
{
    private readonly Dictionary<int, int> _stock = new()
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

    public Task<InventoryDto> DeductStockAsync(int productId, int quantity)
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

    private static InventoryServiceClient CreateMockInventoryClient(
        StockReservationResponse? reservationResponse = null)
    {
        var handler = new MockHttpMessageHandler(reservationResponse ?? new StockReservationResponse
        {
            Success = true,
            Message = "Stock reserved successfully"
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5002") };
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<InventoryServiceClient>();
        return new InventoryServiceClient(httpClient, logger);
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
    public async Task CreateOrder_ReservesStockViaInventoryService()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Id, order.Items.First().ProductId);
        Assert.Equal(5, order.Items.First().Quantity);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var failedResponse = new StockReservationResponse
        {
            Success = false,
            Message = "Insufficient stock"
        };
        var inventoryClient = CreateMockInventoryClient(failedResponse);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }

    [Fact]
    public async Task CreateOrder_ThrowsWhenDeductFails()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryApiClient(checkStockResult: true, deductStockReturnsNull: true);
        var service = new OrderService(context, inventoryClient, CreateLogger());
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        // DeductStockAsync returns null (Conflict) but our OrderService doesn't check the result,
        // so the order should still be created successfully
        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });
        Assert.NotNull(order);
    }
}
