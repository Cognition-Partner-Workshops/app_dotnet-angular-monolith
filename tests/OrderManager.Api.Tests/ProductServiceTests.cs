using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

public class ProductServiceTests
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
    public async Task GetAllProductsAsync_ReturnsAllSeededProducts()
    {
        using var context = CreateContext();
        var service = new ProductService(context);

        var products = await service.GetAllProductsAsync();

        Assert.Equal(5, products.Count);
    }

    [Fact]
    public async Task GetAllProductsAsync_IncludesInventory()
    {
        using var context = CreateContext();
        var service = new ProductService(context);

        var products = await service.GetAllProductsAsync();

        Assert.All(products, p => Assert.NotNull(p.Inventory));
    }

    [Fact]
    public async Task GetProductByIdAsync_ReturnsProduct_WhenExists()
    {
        using var context = CreateContext();
        var service = new ProductService(context);
        var expected = await context.Products.FirstAsync();

        var product = await service.GetProductByIdAsync(expected.Id);

        Assert.NotNull(product);
        Assert.Equal(expected.Id, product.Id);
        Assert.Equal(expected.Name, product.Name);
    }

    [Fact]
    public async Task GetProductByIdAsync_IncludesInventory()
    {
        using var context = CreateContext();
        var service = new ProductService(context);
        var existing = await context.Products.FirstAsync();

        var product = await service.GetProductByIdAsync(existing.Id);

        Assert.NotNull(product);
        Assert.NotNull(product.Inventory);
    }

    [Fact]
    public async Task GetProductByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var context = CreateContext();
        var service = new ProductService(context);

        var product = await service.GetProductByIdAsync(9999);

        Assert.Null(product);
    }

    [Fact]
    public async Task CreateProductAsync_AddsAndReturnsProduct()
    {
        using var context = CreateContext();
        var service = new ProductService(context);
        var newProduct = new Product
        {
            Name = "Test Product",
            Description = "Test Description",
            Category = "TestCategory",
            Price = 99.99m,
            Sku = "TST-001"
        };

        var created = await service.CreateProductAsync(newProduct);

        Assert.NotEqual(0, created.Id);
        Assert.Equal("Test Product", created.Name);
        Assert.Equal("TST-001", created.Sku);
    }

    [Fact]
    public async Task CreateProductAsync_PersistsToDatabase()
    {
        using var context = CreateContext();
        var service = new ProductService(context);
        var newProduct = new Product
        {
            Name = "Persisted Product",
            Description = "Should be saved",
            Category = "TestCategory",
            Price = 50.00m,
            Sku = "TST-002"
        };

        var created = await service.CreateProductAsync(newProduct);
        var fromDb = await context.Products.FindAsync(created.Id);

        Assert.NotNull(fromDb);
        Assert.Equal("Persisted Product", fromDb.Name);
    }

    [Fact]
    public async Task GetProductsByCategoryAsync_ReturnsMatchingProducts()
    {
        using var context = CreateContext();
        var service = new ProductService(context);

        var widgets = await service.GetProductsByCategoryAsync("Widgets");

        Assert.Equal(2, widgets.Count);
        Assert.All(widgets, p => Assert.Equal("Widgets", p.Category));
    }

    [Fact]
    public async Task GetProductsByCategoryAsync_ReturnsEmptyList_WhenCategoryNotFound()
    {
        using var context = CreateContext();
        var service = new ProductService(context);

        var products = await service.GetProductsByCategoryAsync("NonExistentCategory");

        Assert.Empty(products);
    }

    [Fact]
    public async Task GetProductsByCategoryAsync_IncludesInventory()
    {
        using var context = CreateContext();
        var service = new ProductService(context);

        var gadgets = await service.GetProductsByCategoryAsync("Gadgets");

        Assert.NotEmpty(gadgets);
        Assert.All(gadgets, p => Assert.NotNull(p.Inventory));
    }
}
