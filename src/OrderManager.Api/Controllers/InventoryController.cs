using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

/// <summary>
/// API controller for querying and managing product inventory levels.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly InventoryService _inventoryService;

    /// <summary>
    /// Initializes a new instance of <see cref="InventoryController"/>.
    /// </summary>
    /// <param name="inventoryService">The service handling inventory business logic.</param>
    public InventoryController(InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// Returns all inventory items with their associated product details.
    /// </summary>
    /// <returns>A 200 OK response containing a list of all inventory records.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _inventoryService.GetAllInventoryAsync());

    /// <summary>
    /// Returns the inventory record for a specific product.
    /// </summary>
    /// <param name="productId">The identifier of the product to look up.</param>
    /// <returns>A 200 OK response with the inventory item, or 404 Not Found if no record exists for the product.</returns>
    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var item = await _inventoryService.GetInventoryByProductIdAsync(productId);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>
    /// Adds stock to a product's inventory.
    /// </summary>
    /// <param name="productId">The identifier of the product to restock.</param>
    /// <param name="request">The payload containing the quantity to add.</param>
    /// <returns>A 200 OK response with the updated inventory item.</returns>
    [HttpPost("product/{productId}/restock")]
    public async Task<IActionResult> Restock(int productId, [FromBody] RestockRequest request)
    {
        var item = await _inventoryService.RestockAsync(productId, request.Quantity);
        return Ok(item);
    }

    /// <summary>
    /// Returns all inventory items whose on-hand quantity is at or below their reorder level.
    /// </summary>
    /// <returns>A 200 OK response containing a list of low-stock inventory records.</returns>
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock() => Ok(await _inventoryService.GetLowStockItemsAsync());
}

/// <summary>Request payload for restocking a product's inventory.</summary>
/// <param name="Quantity">The number of units to add to the current stock.</param>
public record RestockRequest(int Quantity);
