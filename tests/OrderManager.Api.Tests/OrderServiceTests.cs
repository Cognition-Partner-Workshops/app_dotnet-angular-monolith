using Microsoft.EntityFrameworkCore;
using Xunit;
using OrderManager.Api.Clients;
using OrderManager.Api.Data;
using OrderManager.Api.Services;

namespace OrderManager.Api.Tests;

public class FakeInventoryClient : IInventoryClient
{
    private readonly Dictionary<int, int> _stock = new();

    public FakeInventoryClient(Dictionary<int, int>? initialStock = null)
    {
        _stock = initialStock ?? new Dictionary<int, int>();
    }

    public Task<List<InventoryItemDto>> GetAllInventoryAsync() =>
        Task.FromResult(_stock.Select(kvp => new InventoryItemDto { ProductId = kvp.Key, QuantityOnHand = kvp.Value }).ToList());

    public Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId) =>
        Task.FromResult(_stock.ContainsKey(productId)
            ? new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] }
            : null);

    public Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        _stock[productId] = _stock.GetValueOrDefault(productId) + quantity;
        return Task.FromResult(new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] });
    }

    public Task<List<InventoryItemDto>> GetLowStockItemsAsync() =>
        Task.FromResult(new List<InventoryItemDto>());

    public Task<bool> CheckStockAsync(int productId, int quantity) =>
        Task.FromResult(_stock.ContainsKey(productId) && _stock[productId] >= quantity);

    public Task<InventoryItemDto?> DeductStockAsync(int productId, int quantity)
    {
        if (!_stock.ContainsKey(productId) || _stock[productId] < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");
        _stock[productId] -= quantity;
        return Task.FromResult<InventoryItemDto?>(new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] });
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
        var inventoryClient = new FakeInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_DeductsInventoryViaHttpClient()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var inventoryClient = new FakeInventoryClient(new Dictionary<int, int> { { product.Id, 100 } });
        var service = new OrderService(context, inventoryClient);

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        var stockAvailable = await inventoryClient.CheckStockAsync(product.Id, 95);
        Assert.True(stockAvailable);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var inventoryClient = new FakeInventoryClient(new Dictionary<int, int> { { product.Id, 5 } });
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }

    [Fact]
    public async Task CreateOrder_RollsBackDeductionsOnFailure()
    {
        using var context = CreateContext();
        var products = await context.Products.Take(2).ToListAsync();
        var customer = await context.Customers.FirstAsync();

        // First product has stock, second does not have enough
        var inventoryClient = new FakeInventoryClient(new Dictionary<int, int>
        {
            { products[0].Id, 100 },
            { products[1].Id, 1 }
        });
        var service = new OrderService(context, inventoryClient);

        // Order 10 of product 1 (succeeds deduction) then 99999 of product 2 (fails → triggers rollback)
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)>
            {
                (products[0].Id, 10),
                (products[1].Id, 99999)
            }));

        // Product 1 stock should be restored to 100 after compensation
        var stockRestored = await inventoryClient.CheckStockAsync(products[0].Id, 100);
        Assert.True(stockRestored);
    }
}
