using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Controllers;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

public class CustomersControllerTests
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
    public async Task GetAll_ReturnsOkResult()
    {
        using var context = CreateContext();
        var service = new CustomerService(context);
        var controller = new CustomersController(service);

        var result = await controller.GetAll();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_ReturnsAllCustomers()
    {
        using var context = CreateContext();
        var service = new CustomerService(context);
        var controller = new CustomersController(service);

        var result = await controller.GetAll();
        var okResult = Assert.IsType<OkObjectResult>(result);
        var customers = Assert.IsAssignableFrom<List<Customer>>(okResult.Value);

        Assert.Equal(3, customers.Count);
    }

    [Fact]
    public async Task GetById_ReturnsOkResult_WhenCustomerExists()
    {
        using var context = CreateContext();
        var service = new CustomerService(context);
        var controller = new CustomersController(service);
        var existingCustomer = await context.Customers.FirstAsync();

        var result = await controller.GetById(existingCustomer.Id);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsCorrectCustomer()
    {
        using var context = CreateContext();
        var service = new CustomerService(context);
        var controller = new CustomersController(service);
        var existingCustomer = await context.Customers.FirstAsync();

        var result = await controller.GetById(existingCustomer.Id);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var customer = Assert.IsType<Customer>(okResult.Value);

        Assert.Equal(existingCustomer.Id, customer.Id);
        Assert.Equal(existingCustomer.Name, customer.Name);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenCustomerDoesNotExist()
    {
        using var context = CreateContext();
        var service = new CustomerService(context);
        var controller = new CustomersController(service);

        var result = await controller.GetById(9999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtActionResult()
    {
        using var context = CreateContext();
        var service = new CustomerService(context);
        var controller = new CustomersController(service);
        var newCustomer = new Customer
        {
            Name = "Created Corp",
            Email = "created@corp.com",
            Phone = "555-7777",
            Address = "500 Create Ave",
            City = "Createville",
            State = "WA",
            ZipCode = "98001"
        };

        var result = await controller.Create(newCustomer);

        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedCustomerInResponse()
    {
        using var context = CreateContext();
        var service = new CustomerService(context);
        var controller = new CustomersController(service);
        var newCustomer = new Customer
        {
            Name = "Response Corp",
            Email = "response@corp.com",
            Phone = "555-8888",
            Address = "600 Response Rd",
            City = "Responsetown",
            State = "OR",
            ZipCode = "97001"
        };

        var result = await controller.Create(newCustomer);
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var customer = Assert.IsType<Customer>(createdResult.Value);

        Assert.Equal("Response Corp", customer.Name);
        Assert.Equal("response@corp.com", customer.Email);
        Assert.NotEqual(0, customer.Id);
    }

    [Fact]
    public async Task Create_SetsCorrectRouteValues()
    {
        using var context = CreateContext();
        var service = new CustomerService(context);
        var controller = new CustomersController(service);
        var newCustomer = new Customer
        {
            Name = "Route Corp",
            Email = "route@corp.com",
            Phone = "555-6666",
            Address = "700 Route Way",
            City = "Routetown",
            State = "NV",
            ZipCode = "89001"
        };

        var result = await controller.Create(newCustomer);
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);

        Assert.Equal(nameof(CustomersController.GetById), createdResult.ActionName);
        Assert.NotNull(createdResult.RouteValues);
        Assert.True(createdResult.RouteValues!.ContainsKey("id"));
    }
}
