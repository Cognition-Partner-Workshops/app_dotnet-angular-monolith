using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Models;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

/// <summary>
/// API controller for managing the <see cref="Product"/> catalog.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;

    /// <summary>
    /// Initializes the controller with the product service dependency.
    /// </summary>
    public ProductsController(ProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Returns all products with their current inventory levels.
    /// </summary>
    /// <returns>200 OK with a list of products.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _productService.GetAllProductsAsync());

    /// <summary>
    /// Returns a single product by ID with inventory data.
    /// </summary>
    /// <param name="id">The product's unique identifier.</param>
    /// <returns>200 OK with the product, or 404 Not Found.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>
    /// Returns all products in the specified category.
    /// </summary>
    /// <param name="category">The category name to filter by.</param>
    /// <returns>200 OK with a filtered list of products.</returns>
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category) => Ok(await _productService.GetProductsByCategoryAsync(category));

    /// <summary>
    /// Creates a new product in the catalog.
    /// </summary>
    /// <param name="product">The product data to persist.</param>
    /// <returns>201 Created with a Location header pointing to the new resource.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        var created = await _productService.CreateProductAsync(product);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
