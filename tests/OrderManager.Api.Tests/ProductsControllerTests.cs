using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Controllers;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

public class ProductsControllerTests
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

    private ProductsController CreateController(AppDbContext context)
    {
        var service = new ProductService(context);
        return new ProductsController(service);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithProducts()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var products = Assert.IsAssignableFrom<List<Product>>(okResult.Value);
        Assert.Equal(5, products.Count);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenProductExists()
    {
        using var context = CreateContext();
        var controller = CreateController(context);
        var existing = await context.Products.FirstAsync();

        var result = await controller.GetById(existing.Id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var product = Assert.IsType<Product>(okResult.Value);
        Assert.Equal(existing.Id, product.Id);
        Assert.Equal(existing.Name, product.Name);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenProductMissing()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetById(9999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetByCategory_ReturnsOkWithFilteredProducts()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetByCategory("Gadgets");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var products = Assert.IsAssignableFrom<List<Product>>(okResult.Value);
        Assert.Equal(2, products.Count);
        Assert.All(products, p => Assert.Equal("Gadgets", p.Category));
    }

    [Fact]
    public async Task GetByCategory_ReturnsOkWithEmptyList_WhenCategoryNotFound()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetByCategory("NonExistent");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var products = Assert.IsAssignableFrom<List<Product>>(okResult.Value);
        Assert.Empty(products);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        using var context = CreateContext();
        var controller = CreateController(context);
        var newProduct = new Product
        {
            Name = "Controller Test Product",
            Description = "Created via controller",
            Category = "TestCategory",
            Price = 42.00m,
            Sku = "CTL-001"
        };

        var result = await controller.Create(newProduct);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ProductsController.GetById), createdResult.ActionName);
        var product = Assert.IsType<Product>(createdResult.Value);
        Assert.Equal("Controller Test Product", product.Name);
        Assert.NotEqual(0, product.Id);
    }

    [Fact]
    public async Task Create_PersistsProduct()
    {
        using var context = CreateContext();
        var controller = CreateController(context);
        var newProduct = new Product
        {
            Name = "Persisted Controller Product",
            Description = "Should be in DB",
            Category = "TestCategory",
            Price = 15.00m,
            Sku = "CTL-002"
        };

        await controller.Create(newProduct);

        var fromDb = await context.Products.FirstOrDefaultAsync(p => p.Sku == "CTL-002");
        Assert.NotNull(fromDb);
        Assert.Equal("Persisted Controller Product", fromDb.Name);
    }
}
