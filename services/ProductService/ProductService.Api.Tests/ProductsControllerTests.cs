using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Api.Controllers;
using ProductService.Api.Data;
using ProductService.Api.Models;
using FluentAssertions;
using Xunit;

namespace ProductService.Api.Tests;

public class ProductsControllerTests
{
    private (ProductsController Controller, ProductDbContext Context) CreateController()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new ProductDbContext(options);
        SeedData.Initialize(context);
        var service = new Services.ProductService(context);
        var controller = new ProductsController(service);
        return (controller, context);
    }

    [Fact]
    public async Task GetAll_Returns200WithProducts()
    {
        var (controller, context) = CreateController();
        using var _ = context;

        var result = await controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        var products = okResult.Value.Should().BeAssignableTo<List<Product>>().Subject;
        products.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetById_Returns200_WhenExists()
    {
        var (controller, context) = CreateController();
        using var _ = context;
        var seeded = await context.Products.FirstAsync();

        var result = await controller.GetById(seeded.Id);

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
    public async Task GetByCategory_Returns200WithFilteredProducts()
    {
        var (controller, context) = CreateController();
        using var _ = context;

        var result = await controller.GetByCategory("Widgets");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var products = okResult.Value.Should().BeAssignableTo<List<Product>>().Subject;
        products.Should().HaveCount(2);
    }

    [Fact]
    public async Task Create_Returns201WithCreatedProduct()
    {
        var (controller, context) = CreateController();
        using var _ = context;

        var product = new Product
        {
            Name = "New Product",
            Description = "New",
            Category = "New",
            Price = 9.99m,
            Sku = "NEW-001"
        };

        var result = await controller.Create(product);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Update_Returns200_WhenExists()
    {
        var (controller, context) = CreateController();
        using var _ = context;
        var seeded = await context.Products.FirstAsync();

        var updated = new Product
        {
            Name = "Updated",
            Description = "Updated",
            Category = "Updated",
            Price = 9.99m,
            Sku = "UPD-001"
        };

        var result = await controller.Update(seeded.Id, updated);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_Returns404_WhenNotFound()
    {
        var (controller, context) = CreateController();
        using var _ = context;

        var result = await controller.Update(9999, new Product { Name = "X", Sku = "X" });

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_Returns204_WhenExists()
    {
        var (controller, context) = CreateController();
        using var _ = context;
        var seeded = await context.Products.FirstAsync();

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
