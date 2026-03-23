using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

/// <summary>
/// REST API controller for managing warehouse inventory.
/// Provides endpoints for querying stock levels, restocking products, and identifying low-stock items.
/// Route: api/inventory
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    /// <summary>
    /// The inventory service used to perform business logic operations.
    /// </summary>
    private readonly InventoryService _inventoryService;

    /// <summary>
    /// Initializes a new instance of <see cref="InventoryController"/> with the specified service.
    /// </summary>
    /// <param name="inventoryService">The inventory service injected via dependency injection.</param>
    public InventoryController(InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// Retrieves all inventory items with their associated product details.
    /// </summary>
    /// <returns>An HTTP 200 response containing a list of all inventory items.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _inventoryService.GetAllInventoryAsync());

    /// <summary>
    /// Retrieves the inventory record for a specific product.
    /// </summary>
    /// <param name="productId">The unique identifier of the product.</param>
    /// <returns>An HTTP 200 response with the inventory data, or HTTP 404 if no inventory record exists for the product.</returns>
    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var item = await _inventoryService.GetInventoryByProductIdAsync(productId);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>
    /// Restocks a product by adding the specified quantity to its current inventory level.
    /// </summary>
    /// <param name="productId">The unique identifier of the product to restock.</param>
    /// <param name="request">The restock request containing the quantity to add.</param>
    /// <returns>An HTTP 200 response with the updated inventory item.</returns>
    [HttpPost("product/{productId}/restock")]
    public async Task<IActionResult> Restock(int productId, [FromBody] RestockRequest request)
    {
        var item = await _inventoryService.RestockAsync(productId, request.Quantity);
        return Ok(item);
    }

    /// <summary>
    /// Retrieves all inventory items that are at or below their reorder threshold.
    /// </summary>
    /// <returns>An HTTP 200 response containing a list of low-stock inventory items.</returns>
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock() => Ok(await _inventoryService.GetLowStockItemsAsync());
}

/// <summary>
/// Request model for restocking a product's inventory.
/// </summary>
/// <param name="Quantity">The number of units to add to the product's current stock level.</param>
public record RestockRequest(int Quantity);
