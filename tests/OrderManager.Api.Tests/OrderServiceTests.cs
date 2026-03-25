using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

public class FakeInventoryServiceClient : IInventoryServiceClient
{
    private readonly Dictionary<int, int> _stock = new();
    private readonly bool _shouldFailReservation;

    public FakeInventoryServiceClient(
        Dictionary<int, int>? initialStock = null,
        bool shouldFailReservation = false)
    {
        _stock = initialStock ?? new Dictionary<int, int>();
        _shouldFailReservation = shouldFailReservation;
    }

    public Task<List<InventoryItemDto>> GetAllInventoryAsync() =>
        Task.FromResult(_stock.Select(kvp => new InventoryItemDto
        {
            ProductId = kvp.Key,
            QuantityOnHand = kvp.Value
        }).ToList());

    public Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId) =>
        Task.FromResult(_stock.ContainsKey(productId)
            ? new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] }
            : null);

    public Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        _stock[productId] = _stock.GetValueOrDefault(productId) + quantity;
        return Task.FromResult(new InventoryItemDto
        {
            ProductId = productId,
            QuantityOnHand = _stock[productId]
        });
    }

    public Task<List<InventoryItemDto>> GetLowStockItemsAsync() =>
        Task.FromResult(new List<InventoryItemDto>());

    public Task<StockReservationResponse> CheckAndReserveStockAsync(StockReservationRequest request)
    {
        if (_shouldFailReservation)
        {
            return Task.FromResult(new StockReservationResponse
            {
                Success = false,
                Message = "Insufficient stock"
            });
        }

        foreach (var item in request.Items)
        {
            if (!_stock.ContainsKey(item.ProductId) || _stock[item.ProductId] < item.Quantity)
            {
                return Task.FromResult(new StockReservationResponse
                {
                    Success = false,
                    Message = $"Insufficient stock for product {item.ProductId}"
                });
            }
        }

        foreach (var item in request.Items)
            _stock[item.ProductId] -= item.Quantity;

        return Task.FromResult(new StockReservationResponse
        {
            Success = true,
            Message = "Stock reserved successfully"
        });
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
    public async Task CreateOrder_ReservesStockViaInventoryService()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var inventoryClient = new FakeInventoryServiceClient(
            new Dictionary<int, int> { { product.Id, 100 } });
        var service = new OrderService(context, inventoryClient);

        var order = await service.CreateOrderAsync(
            customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Price * 5, order.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnFailedReservation()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var inventoryClient = new FakeInventoryServiceClient(
            new Dictionary<int, int> { { product.Id, 2 } },
            shouldFailReservation: true);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(
                customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }

    [Fact]
    public async Task CreateOrder_ThrowsWhenInsufficientStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var inventoryClient = new FakeInventoryServiceClient(
            new Dictionary<int, int> { { product.Id, 2 } });
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(
                customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
