using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Models;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;
    private readonly InventoryHttpClient _inventoryClient;

    public ProductsController(ProductService productService, InventoryHttpClient inventoryClient)
    {
        _productService = productService;
        _inventoryClient = inventoryClient;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _productService.GetAllProductsAsync();
        var inventory = await _inventoryClient.GetAllInventoryAsync();
        var inventoryByProduct = inventory.ToDictionary(i => i.ProductId);
        var result = products.Select(p => new ProductWithStockDto(p, inventoryByProduct.GetValueOrDefault(p.Id)));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product is null) return NotFound();
        var inv = await _inventoryClient.GetInventoryByProductIdAsync(id);
        return Ok(new ProductWithStockDto(product, inv));
    }

    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category)
    {
        var products = await _productService.GetProductsByCategoryAsync(category);
        var inventory = await _inventoryClient.GetAllInventoryAsync();
        var inventoryByProduct = inventory.ToDictionary(i => i.ProductId);
        var result = products.Select(p => new ProductWithStockDto(p, inventoryByProduct.GetValueOrDefault(p.Id)));
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        var created = await _productService.CreateProductAsync(product);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}

/// <summary>
/// DTO that preserves the original API shape with an "inventory" property
/// so the Angular frontend's {{p.inventory?.quantityOnHand}} continues to work.
/// </summary>
public class ProductWithStockDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Sku { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public InventoryStockDto? Inventory { get; set; }

    public ProductWithStockDto(Product product, InventoryItemDto? inventoryItem)
    {
        Id = product.Id;
        Name = product.Name;
        Description = product.Description;
        Category = product.Category;
        Price = product.Price;
        Sku = product.Sku;
        CreatedAt = product.CreatedAt;
        Inventory = inventoryItem is not null
            ? new InventoryStockDto { QuantityOnHand = inventoryItem.QuantityOnHand, ReorderLevel = inventoryItem.ReorderLevel }
            : null;
    }
}

public class InventoryStockDto
{
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }
}
