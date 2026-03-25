using Microsoft.EntityFrameworkCore;
using Moq;
using OrderManager.Api.Clients;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

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

    private Mock<IInventoryClient> CreateMockInventoryClient(bool deductSuccess = true)
    {
        var mock = new Mock<IInventoryClient>();
        if (deductSuccess)
        {
            mock.Setup(c => c.DeductStockAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new InventoryItemDto { ProductId = 1, QuantityOnHand = 45 });
        }
        else
        {
            mock.Setup(c => c.DeductStockAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("Insufficient stock"));
        }
        return mock;
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var mockClient = CreateMockInventoryClient();
        var service = new OrderService(context, mockClient.Object);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryServiceToDeductStock()
    {
        using var context = CreateContext();
        var mockClient = CreateMockInventoryClient(deductSuccess: true);
        var service = new OrderService(context, mockClient.Object);
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
        Assert.Equal(product.Id, order.Items.First().ProductId);
        mockClient.Verify(c => c.DeductStockAsync(product.Id, 5), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var mockClient = CreateMockInventoryClient(deductSuccess: false);
        var service = new OrderService(context, mockClient.Object);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

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
