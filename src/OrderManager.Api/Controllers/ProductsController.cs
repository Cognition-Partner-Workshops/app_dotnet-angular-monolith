using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Models;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

/// <summary>
/// API controller for managing the product catalog.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;

    /// <summary>
    /// Initializes a new instance of <see cref="ProductsController"/>.
    /// </summary>
    /// <param name="productService">The service handling product business logic.</param>
    public ProductsController(ProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Returns all products with their current inventory levels.
    /// </summary>
    /// <returns>A 200 OK response containing a list of all products.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _productService.GetAllProductsAsync());

    /// <summary>
    /// Returns a single product by its identifier, including inventory data.
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <returns>A 200 OK response with the product, or 404 Not Found if the product does not exist.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>
    /// Returns all products belonging to the specified category.
    /// </summary>
    /// <param name="category">The category name to filter by.</param>
    /// <returns>A 200 OK response containing the matching products.</returns>
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category) => Ok(await _productService.GetProductsByCategoryAsync(category));

    /// <summary>
    /// Creates a new product in the catalog.
    /// </summary>
    /// <param name="product">The product data to create.</param>
    /// <returns>A 201 Created response with the new product and a Location header pointing to the new resource.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        var created = await _productService.CreateProductAsync(product);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
