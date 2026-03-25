using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Models;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

/// <summary>
/// REST endpoints for browsing and managing the product catalog.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;

    public ProductsController(ProductService productService)
    {
        _productService = productService;
    }

    /// <summary>GET /api/products — returns all products with inventory levels.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _productService.GetAllProductsAsync());

    /// <summary>GET /api/products/{id} — returns a single product or 404.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>GET /api/products/category/{category} — filters by category name.</summary>
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category) => Ok(await _productService.GetProductsByCategoryAsync(category));

    /// <summary>POST /api/products — adds a new product to the catalog and returns 201.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        var created = await _productService.CreateProductAsync(product);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
