using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using OrderService.Api.Clients;
using OrderService.Api.Controllers;
using OrderService.Api.Data;
using FluentAssertions;
using Xunit;

namespace OrderService.Api.Tests;

public class OrdersControllerTests
{
    private (OrdersController Controller, OrderDbContext Context) CreateController()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new OrderDbContext(options);

        var customerMock = new Mock<ICustomerApiClient>();
        customerMock.Setup(c => c.GetCustomerAsync(It.IsAny<int>()))
            .ReturnsAsync(new CustomerDto { Id = 1, Name = "Test", Email = "t@t.com", Address = "123 St", City = "City", State = "ST", ZipCode = "00000" });
        customerMock.Setup(c => c.GetCustomerAddressAsync(It.IsAny<int>()))
            .ReturnsAsync(new CustomerAddressDto { FullAddress = "123 St, City, ST 00000" });

        var productMock = new Mock<IProductApiClient>();
        productMock.Setup(p => p.GetProductAsync(It.IsAny<int>()))
            .ReturnsAsync(new ProductDto { Id = 1, Name = "Test", Price = 10m, Sku = "T-1" });

        var inventoryMock = new Mock<IInventoryApiClient>();
        inventoryMock.Setup(i => i.ReserveStockAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new ReserveStockResponse { Success = true, Message = "OK" });

        var service = new Services.OrderService(context, customerMock.Object, productMock.Object, inventoryMock.Object);
        var controller = new OrdersController(service);
        return (controller, context);
    }

    [Fact]
    public async Task GetAll_Returns200()
    {
        var (controller, context) = CreateController();
        using var _ = context;

        var result = await controller.GetAll();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotFound()
    {
        var (controller, context) = CreateController();
        using var _ = context;

        var result = await controller.GetById(9999);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_Returns201()
    {
        var (controller, context) = CreateController();
        using var _ = context;

        var request = new CreateOrderRequest(1, new List<OrderItemRequest> { new(1, 2) });

        var result = await controller.Create(request);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task UpdateStatus_Returns404_WhenNotFound()
    {
        var (controller, context) = CreateController();
        using var _ = context;

        var result = await controller.UpdateStatus(9999, new UpdateStatusRequest("Shipped"));

        result.Should().BeOfType<NotFoundResult>();
    }
}
