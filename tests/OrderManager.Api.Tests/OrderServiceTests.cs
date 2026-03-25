using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

public class FakeInventoryHandler : HttpMessageHandler
{
    private readonly Dictionary<int, int> _stockLevels = new();
    private readonly bool _deductSuccess;

    public FakeInventoryHandler(Dictionary<int, int> stockLevels, bool deductSuccess = true)
    {
        _stockLevels = stockLevels;
        _deductSuccess = deductSuccess;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        if (path.Contains("/stock-level"))
        {
            var productId = int.Parse(path.Split("/product/")[1].Split("/")[0]);
            var level = _stockLevels.GetValueOrDefault(productId, 0);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { productId, quantityOnHand = level })
            };
            return Task.FromResult(response);
        }

        if (path.Contains("/deduct"))
        {
            var status = _deductSuccess ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            var response = new HttpResponseMessage(status)
            {
                Content = JsonContent.Create(new { message = _deductSuccess ? "Stock deducted" : "Insufficient stock" })
            };
            return Task.FromResult(response);
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
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

    private InventoryHttpClient CreateInventoryClient(Dictionary<int, int> stockLevels, bool deductSuccess = true)
    {
        var handler = new FakeInventoryHandler(stockLevels, deductSuccess);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5002") };
        return new InventoryHttpClient(httpClient);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = CreateInventoryClient(new Dictionary<int, int>());
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryService()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryClient(stockAvailable: true);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var stockLevels = new Dictionary<int, int> { { product.Id, 100 } };
        var inventoryClient = CreateInventoryClient(stockLevels);
        var service = new OrderService(context, inventoryClient);

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Id, order.Items.First().ProductId);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryClient(stockAvailable: false);
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
