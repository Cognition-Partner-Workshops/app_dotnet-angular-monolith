using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CustomerService.Api.Controllers;
using CustomerService.Api.Data;
using CustomerService.Api.Models;
using FluentAssertions;
using Xunit;

namespace CustomerService.Api.Tests;

public class CustomersControllerTests
{
    private (CustomersController Controller, CustomerDbContext Context) CreateController()
    {
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new CustomerDbContext(options);
        SeedData.Initialize(context);
        var service = new Services.CustomerService(context);
        var controller = new CustomersController(service);
        return (controller, context);
    }

    [Fact]
    public async Task GetAll_Returns200WithCustomers()
    {
        var (controller, context) = CreateController();
        using var _ = context;

        var result = await controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        var customers = okResult.Value.Should().BeAssignableTo<List<Customer>>().Subject;
        customers.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetById_Returns200_WhenExists()
    {
        var (controller, context) = CreateController();
        using var _ = context;
        var seeded = await context.Customers.FirstAsync();

        var result = await controller.GetById(seeded.Id);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
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
    public async Task GetAddress_Returns200WithAddressFields()
    {
        var (controller, context) = CreateController();
        using var _ = context;
        var seeded = await context.Customers.FirstAsync();

        var result = await controller.GetAddress(seeded.Id);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAddress_Returns404_WhenNotFound()
    {
        var (controller, context) = CreateController();
        using var _ = context;

        var result = await controller.GetAddress(9999);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_Returns201WithCreatedCustomer()
    {
        var (controller, context) = CreateController();
        using var _ = context;

        var customer = new Customer
        {
            Name = "New Corp",
            Email = "new@corp.com",
            Phone = "555-1111",
            Address = "111 New St",
            City = "Newtown",
            State = "NY",
            ZipCode = "10001"
        };

        var result = await controller.Create(customer);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Update_Returns200_WhenExists()
    {
        var (controller, context) = CreateController();
        using var _ = context;
        var seeded = await context.Customers.FirstAsync();

        var updated = new Customer
        {
            Name = "Updated",
            Email = "updated@test.com",
            Phone = "555-0000",
            Address = "Updated Addr",
            City = "Updated City",
            State = "CA",
            ZipCode = "90001"
        };

        var result = await controller.Update(seeded.Id, updated);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_Returns404_WhenNotFound()
    {
        var (controller, context) = CreateController();
        using var _ = context;

        var result = await controller.Update(9999, new Customer { Name = "X", Email = "x@x.com" });

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_Returns204_WhenExists()
    {
        var (controller, context) = CreateController();
        using var _ = context;
        var seeded = await context.Customers.FirstAsync();

        var result = await controller.Delete(seeded.Id);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Returns404_WhenNotFound()
    {
        var (controller, context) = CreateController();
        using var _ = context;

        var result = await controller.Delete(9999);

        result.Should().BeOfType<NotFoundResult>();
    }
}
