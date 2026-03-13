using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Controllers;
using OrderManager.Api.Data;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

public class OrdersControllerTests
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
    public async Task GetAll_ReturnsOkWithList()
    {
        using var context = CreateContext();
        var service = new OrderService(context);
        var controller = new OrdersController(service);

        var result = await controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetById_ReturnsOk_ForExistingOrder()
    {
        using var context = CreateContext();
        var service = new OrderService(context);
        var controller = new OrdersController(service);

        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 1) });

        var result = await controller.GetById(order.Id);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_ForMissingOrder()
    {
        using var context = CreateContext();
        var service = new OrderService(context);
        var controller = new OrdersController(service);

        var result = await controller.GetById(99999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtActionWithOrder()
    {
        using var context = CreateContext();
        var service = new OrderService(context);
        var controller = new OrdersController(service);

        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var request = new CreateOrderRequest(
            customer.Id,
            new List<OrderItemRequest> { new OrderItemRequest(product.Id, 2) });

        var result = await controller.Create(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.NotNull(createdResult.Value);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsOkWithUpdatedOrder()
    {
        using var context = CreateContext();
        var service = new OrderService(context);
        var controller = new OrdersController(service);

        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 1) });

        var result = await controller.UpdateStatus(order.Id, new UpdateStatusRequest("Shipped"));

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}
