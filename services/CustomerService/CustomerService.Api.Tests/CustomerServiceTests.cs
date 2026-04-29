using Microsoft.EntityFrameworkCore;
using CustomerService.Api.Data;
using CustomerService.Api.Models;
using FluentAssertions;
using Xunit;

namespace CustomerService.Api.Tests;

public class CustomerServiceTests
{
    private CustomerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new CustomerDbContext(options);
        SeedData.Initialize(context);
        return context;
    }

    [Fact]
    public async Task GetAllCustomers_ReturnsSeedData()
    {
        using var context = CreateContext();
        var service = new Services.CustomerService(context);

        var customers = await service.GetAllCustomersAsync();

        customers.Should().HaveCount(3);
        customers.Select(c => c.Name).Should().Contain("Acme Corp");
    }

    [Fact]
    public async Task GetCustomerById_ReturnsCustomer_WhenExists()
    {
        using var context = CreateContext();
        var service = new Services.CustomerService(context);
        var seeded = await context.Customers.FirstAsync();

        var customer = await service.GetCustomerByIdAsync(seeded.Id);

        customer.Should().NotBeNull();
        customer!.Name.Should().Be(seeded.Name);
        customer.Email.Should().Be(seeded.Email);
    }

    [Fact]
    public async Task GetCustomerById_ReturnsNull_WhenNotFound()
    {
        using var context = CreateContext();
        var service = new Services.CustomerService(context);

        var customer = await service.GetCustomerByIdAsync(9999);

        customer.Should().BeNull();
    }

    [Fact]
    public async Task CreateCustomer_PersistsAndReturns()
    {
        using var context = CreateContext();
        var service = new Services.CustomerService(context);

        var newCustomer = new Customer
        {
            Name = "Test Corp",
            Email = "test@test.com",
            Phone = "555-9999",
            Address = "999 Test St",
            City = "Testville",
            State = "TX",
            ZipCode = "75001"
        };

        var created = await service.CreateCustomerAsync(newCustomer);

        created.Id.Should().BeGreaterThan(0);
        created.Name.Should().Be("Test Corp");

        var fromDb = await context.Customers.FindAsync(created.Id);
        fromDb.Should().NotBeNull();
        fromDb!.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task UpdateCustomer_ModifiesFields()
    {
        using var context = CreateContext();
        var service = new Services.CustomerService(context);
        var existing = await context.Customers.FirstAsync();

        var updated = new Customer
        {
            Name = "Updated Name",
            Email = "updated@test.com",
            Phone = "555-0000",
            Address = "Updated Address",
            City = "Updated City",
            State = "CA",
            ZipCode = "90001"
        };

        var result = await service.UpdateCustomerAsync(existing.Id, updated);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.Email.Should().Be("updated@test.com");
        result.City.Should().Be("Updated City");
    }

    [Fact]
    public async Task DeleteCustomer_RemovesFromDb()
    {
        using var context = CreateContext();
        var service = new Services.CustomerService(context);
        var existing = await context.Customers.FirstAsync();

        var deleted = await service.DeleteCustomerAsync(existing.Id);

        deleted.Should().BeTrue();
        var fromDb = await context.Customers.FindAsync(existing.Id);
        fromDb.Should().BeNull();
    }
}
