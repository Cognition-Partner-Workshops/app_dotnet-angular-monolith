using Microsoft.EntityFrameworkCore;
using ProductService.Api.Data;
using ProductService.Api.Models;
using FluentAssertions;
using Xunit;

namespace ProductService.Api.Tests;

public class ProductServiceTests
{
    private ProductDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new ProductDbContext(options);
        SeedData.Initialize(context);
        return context;
    }

    [Fact]
    public async Task GetAllProducts_ReturnsSeedData()
    {
        using var context = CreateContext();
        var service = new Services.ProductService(context);

        var products = await service.GetAllProductsAsync();

        products.Should().HaveCount(5);
        products.Select(p => p.Name).Should().Contain("Widget A");
    }

    [Fact]
    public async Task GetProductById_ReturnsProduct_WhenExists()
    {
        using var context = CreateContext();
        var service = new Services.ProductService(context);
        var seeded = await context.Products.FirstAsync();

        var product = await service.GetProductByIdAsync(seeded.Id);

        product.Should().NotBeNull();
        product!.Name.Should().Be(seeded.Name);
        product.Sku.Should().Be(seeded.Sku);
    }

    [Fact]
    public async Task GetProductById_ReturnsNull_WhenNotFound()
    {
        using var context = CreateContext();
        var service = new Services.ProductService(context);

        var product = await service.GetProductByIdAsync(9999);

        product.Should().BeNull();
    }

    [Fact]
    public async Task CreateProduct_PersistsAndReturns()
    {
        using var context = CreateContext();
        var service = new Services.ProductService(context);

        var newProduct = new Product
        {
            Name = "Test Product",
            Description = "Test Description",
            Category = "Test",
            Price = 99.99m,
            Sku = "TST-001"
        };

        var created = await service.CreateProductAsync(newProduct);

        created.Id.Should().BeGreaterThan(0);
        created.Name.Should().Be("Test Product");

        var fromDb = await context.Products.FindAsync(created.Id);
        fromDb.Should().NotBeNull();
        fromDb!.Price.Should().Be(99.99m);
    }

    [Fact]
    public async Task UpdateProduct_ModifiesFields()
    {
        using var context = CreateContext();
        var service = new Services.ProductService(context);
        var existing = await context.Products.FirstAsync();

        var updated = new Product
        {
            Name = "Updated Product",
            Description = "Updated Description",
            Category = "Updated",
            Price = 199.99m,
            Sku = "UPD-001"
        };

        var result = await service.UpdateProductAsync(existing.Id, updated);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Product");
        result.Price.Should().Be(199.99m);
    }

    [Fact]
    public async Task DeleteProduct_RemovesFromDb()
    {
        using var context = CreateContext();
        var service = new Services.ProductService(context);
        var existing = await context.Products.FirstAsync();

        var deleted = await service.DeleteProductAsync(existing.Id);

        deleted.Should().BeTrue();
        var fromDb = await context.Products.FindAsync(existing.Id);
        fromDb.Should().BeNull();
    }
}
