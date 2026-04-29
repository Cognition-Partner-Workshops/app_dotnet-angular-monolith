using Microsoft.EntityFrameworkCore;
using Moq;
using OrderService.Api.Clients;
using OrderService.Api.Data;
using OrderService.Api.Models;
using FluentAssertions;
using Xunit;

namespace OrderService.Api.Tests;

public class OrderServiceTests
{
    private OrderDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new OrderDbContext(options);
    }

    private static Mock<ICustomerApiClient> CreateMockCustomerClient(CustomerDto? customer = null)
    {
        var mock = new Mock<ICustomerApiClient>();
        customer ??= new CustomerDto
        {
            Id = 1,
            Name = "Acme Corp",
            Email = "orders@acme.com",
            Address = "123 Main St",
            City = "Springfield",
            State = "IL",
            ZipCode = "62701"
        };
        mock.Setup(c => c.GetCustomerAsync(It.IsAny<int>())).ReturnsAsync(customer);
        mock.Setup(c => c.GetCustomerAddressAsync(It.IsAny<int>()))
            .ReturnsAsync(new CustomerAddressDto
            {
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                ZipCode = customer.ZipCode,
                FullAddress = $"{customer.Address}, {customer.City}, {customer.State} {customer.ZipCode}"
            });
        return mock;
    }

    private static Mock<IProductApiClient> CreateMockProductClient()
    {
        var mock = new Mock<IProductApiClient>();
        mock.Setup(p => p.GetProductAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => new ProductDto
            {
                Id = id,
                Name = $"Product {id}",
                Description = "Test",
                Category = "Test",
                Price = 9.99m,
                Sku = $"SKU-{id}"
            });
        return mock;
    }

    private static Mock<IInventoryApiClient> CreateMockInventoryClient(bool reserveSuccess = true)
    {
        var mock = new Mock<IInventoryApiClient>();
        mock.Setup(i => i.ReserveStockAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new ReserveStockResponse { Success = reserveSuccess, Message = reserveSuccess ? "Reserved" : "Insufficient stock" });
        mock.Setup(i => i.ReleaseStockAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new ReleaseStockResponse { Success = true, Message = "Released" });
        return mock;
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var service = new Services.OrderService(
            context,
            CreateMockCustomerClient().Object,
            CreateMockProductClient().Object,
            CreateMockInventoryClient().Object);

        var orders = await service.GetAllOrdersAsync();

        orders.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateOrder_Success_StoresOrderAndItems()
    {
        using var context = CreateContext();
        var customerMock = CreateMockCustomerClient();
        var productMock = CreateMockProductClient();
        var inventoryMock = CreateMockInventoryClient();

        var service = new Services.OrderService(context, customerMock.Object, productMock.Object, inventoryMock.Object);

        var order = await service.CreateOrderAsync(1, new List<(int, int)> { (1, 5) });

        order.Id.Should().BeGreaterThan(0);
        order.CustomerId.Should().Be(1);
        order.ShippingAddress.Should().Contain("123 Main St");
        order.TotalAmount.Should().Be(49.95m);
        order.Items.Should().HaveCount(1);
        order.Items.First().Quantity.Should().Be(5);
        order.Items.First().UnitPrice.Should().Be(9.99m);
    }

    [Fact]
    public async Task CreateOrder_DeductsInventory()
    {
        using var context = CreateContext();
        var inventoryMock = CreateMockInventoryClient();

        var service = new Services.OrderService(
            context,
            CreateMockCustomerClient().Object,
            CreateMockProductClient().Object,
            inventoryMock.Object);

        await service.CreateOrderAsync(1, new List<(int, int)> { (1, 5) });

        inventoryMock.Verify(i => i.ReserveStockAsync(1, 5), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var inventoryMock = CreateMockInventoryClient(reserveSuccess: false);

        var service = new Services.OrderService(
            context,
            CreateMockCustomerClient().Object,
            CreateMockProductClient().Object,
            inventoryMock.Object);

        var act = () => service.CreateOrderAsync(1, new List<(int, int)> { (1, 99999) });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Insufficient stock*");
    }

    [Fact]
    public async Task CreateOrder_CustomerNotFound_Throws()
    {
        using var context = CreateContext();
        var customerMock = new Mock<ICustomerApiClient>();
        customerMock.Setup(c => c.GetCustomerAsync(It.IsAny<int>())).ReturnsAsync((CustomerDto?)null);

        var service = new Services.OrderService(
            context,
            customerMock.Object,
            CreateMockProductClient().Object,
            CreateMockInventoryClient().Object);

        var act = () => service.CreateOrderAsync(999, new List<(int, int)> { (1, 1) });

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Customer 999 not found*");
    }

    [Fact]
    public async Task CreateOrder_ProductNotFound_Throws()
    {
        using var context = CreateContext();
        var productMock = new Mock<IProductApiClient>();
        productMock.Setup(p => p.GetProductAsync(It.IsAny<int>())).ReturnsAsync((ProductDto?)null);

        var service = new Services.OrderService(
            context,
            CreateMockCustomerClient().Object,
            productMock.Object,
            CreateMockInventoryClient().Object);

        var act = () => service.CreateOrderAsync(1, new List<(int, int)> { (999, 1) });

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Product 999 not found*");
    }

    [Fact]
    public async Task CreateOrder_CompensatesOnPartialFailure()
    {
        using var context = CreateContext();
        var inventoryMock = new Mock<IInventoryApiClient>();

        var callCount = 0;
        inventoryMock.Setup(i => i.ReserveStockAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                    return new ReserveStockResponse { Success = true, Message = "Reserved" };
                return new ReserveStockResponse { Success = false, Message = "Insufficient stock" };
            });
        inventoryMock.Setup(i => i.ReleaseStockAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new ReleaseStockResponse { Success = true, Message = "Released" });

        var service = new Services.OrderService(
            context,
            CreateMockCustomerClient().Object,
            CreateMockProductClient().Object,
            inventoryMock.Object);

        var act = () => service.CreateOrderAsync(1, new List<(int, int)> { (1, 5), (2, 5) });

        await act.Should().ThrowAsync<InvalidOperationException>();

        inventoryMock.Verify(i => i.ReleaseStockAsync(1, 5), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatus_UpdatesStatus()
    {
        using var context = CreateContext();
        var service = new Services.OrderService(
            context,
            CreateMockCustomerClient().Object,
            CreateMockProductClient().Object,
            CreateMockInventoryClient().Object);

        var order = await service.CreateOrderAsync(1, new List<(int, int)> { (1, 1) });

        var updated = await service.UpdateOrderStatusAsync(order.Id, "Shipped");

        updated.Status.Should().Be("Shipped");
    }
}
