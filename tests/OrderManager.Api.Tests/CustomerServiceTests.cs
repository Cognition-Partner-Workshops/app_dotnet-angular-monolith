using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

public class CustomerServiceTests
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
    public async Task GetAllCustomersAsync_ReturnsAllSeededCustomers()
    {
        using var context = CreateContext();
        var service = new CustomerService(context);

        var customers = await service.GetAllCustomersAsync();

        Assert.Equal(3, customers.Count);
    }

    [Fact]
    public async Task GetAllCustomersAsync_ContainsExpectedCustomerNames()
    {
        using var context = CreateContext();
        var service = new CustomerService(context);

        var customers = await service.GetAllCustomersAsync();
        var names = customers.Select(c => c.Name).ToList();

        Assert.Contains("Acme Corp", names);
        Assert.Contains("Globex Inc", names);
        Assert.Contains("Initech LLC", names);
    }

    [Fact]
    public async Task GetCustomerByIdAsync_ReturnsCustomer_WhenExists()
    {
        using var context = CreateContext();
        var service = new CustomerService(context);
        var firstCustomer = await context.Customers.FirstAsync();

        var result = await service.GetCustomerByIdAsync(firstCustomer.Id);

        Assert.NotNull(result);
        Assert.Equal(firstCustomer.Id, result.Id);
        Assert.Equal(firstCustomer.Name, result.Name);
        Assert.Equal(firstCustomer.Email, result.Email);
    }

    [Fact]
    public async Task GetCustomerByIdAsync_IncludesOrders()
    {
        using var context = CreateContext();
        var service = new CustomerService(context);
        var customer = await context.Customers.FirstAsync();

        var result = await service.GetCustomerByIdAsync(customer.Id);

        Assert.NotNull(result);
        Assert.NotNull(result.Orders);
    }

    [Fact]
    public async Task GetCustomerByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var context = CreateContext();
        var service = new CustomerService(context);

        var result = await service.GetCustomerByIdAsync(9999);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateCustomerAsync_AddsCustomerToDatabase()
    {
        using var context = CreateContext();
        var service = new CustomerService(context);
        var newCustomer = new Customer
        {
            Name = "Test Corp",
            Email = "test@testcorp.com",
            Phone = "555-9999",
            Address = "100 Test Blvd",
            City = "Testville",
            State = "TX",
            ZipCode = "75001"
        };

        var result = await service.CreateCustomerAsync(newCustomer);

        Assert.NotEqual(0, result.Id);
        Assert.Equal("Test Corp", result.Name);
        Assert.Equal("test@testcorp.com", result.Email);
    }

    [Fact]
    public async Task CreateCustomerAsync_IncreasesCustomerCount()
    {
        using var context = CreateContext();
        var service = new CustomerService(context);
        var countBefore = await context.Customers.CountAsync();
        var newCustomer = new Customer
        {
            Name = "New Corp",
            Email = "new@newcorp.com",
            Phone = "555-0000",
            Address = "200 New St",
            City = "Newtown",
            State = "NY",
            ZipCode = "10001"
        };

        await service.CreateCustomerAsync(newCustomer);

        var countAfter = await context.Customers.CountAsync();
        Assert.Equal(countBefore + 1, countAfter);
    }

    [Fact]
    public async Task CreateCustomerAsync_CustomerIsPersisted()
    {
        using var context = CreateContext();
        var service = new CustomerService(context);
        var newCustomer = new Customer
        {
            Name = "Persisted Corp",
            Email = "persist@corp.com",
            Phone = "555-1111",
            Address = "300 Persist Ln",
            City = "Datatown",
            State = "CA",
            ZipCode = "90001"
        };

        var created = await service.CreateCustomerAsync(newCustomer);
        var fetched = await context.Customers.FindAsync(created.Id);

        Assert.NotNull(fetched);
        Assert.Equal("Persisted Corp", fetched.Name);
        Assert.Equal("persist@corp.com", fetched.Email);
    }
}
