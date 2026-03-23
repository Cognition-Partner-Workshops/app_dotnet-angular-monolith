using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Models;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

/// <summary>
/// REST API controller for managing the product catalog.
/// Provides endpoints for listing, retrieving by ID or category, and creating products.
/// Route: api/products
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    /// <summary>
    /// The product service used to perform business logic operations.
    /// </summary>
    private readonly ProductService _productService;

    /// <summary>
    /// Initializes a new instance of <see cref="ProductsController"/> with the specified service.
    /// </summary>
    /// <param name="productService">The product service injected via dependency injection.</param>
    public ProductsController(ProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Retrieves all products, including their current inventory levels.
    /// </summary>
    /// <returns>An HTTP 200 response containing a list of all products.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _productService.GetAllProductsAsync());

    /// <summary>
    /// Retrieves a single product by its unique identifier, including its inventory record.
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <returns>An HTTP 200 response with the product data, or HTTP 404 if the product is not found.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>
    /// Retrieves all products in the specified category.
    /// </summary>
    /// <param name="category">The category name to filter by (exact match).</param>
    /// <returns>An HTTP 200 response containing a list of products in the specified category.</returns>
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category) => Ok(await _productService.GetProductsByCategoryAsync(category));

    /// <summary>
    /// Creates a new product in the catalog.
    /// </summary>
    /// <param name="product">The product data provided in the request body.</param>
    /// <returns>An HTTP 201 response with the created product and a Location header pointing to the new resource.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        var created = await _productService.CreateProductAsync(product);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
