using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

/// <summary>
/// API controller for viewing and managing product inventory levels.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly InventoryService _inventoryService;

    /// <summary>
    /// Initializes the controller with the inventory service dependency.
    /// </summary>
    public InventoryController(InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// Returns all inventory records with their associated product data.
    /// </summary>
    /// <returns>200 OK with a list of inventory items.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _inventoryService.GetAllInventoryAsync());

    /// <summary>
    /// Returns the inventory record for a specific product.
    /// </summary>
    /// <param name="productId">The product's unique identifier.</param>
    /// <returns>200 OK with the inventory item, or 404 Not Found.</returns>
    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var item = await _inventoryService.GetInventoryByProductIdAsync(productId);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>
    /// Adds stock to a product's inventory.
    /// </summary>
    /// <param name="productId">The product to restock.</param>
    /// <param name="request">The number of units to add.</param>
    /// <returns>200 OK with the updated inventory item.</returns>
    [HttpPost("product/{productId}/restock")]
    public async Task<IActionResult> Restock(int productId, [FromBody] RestockRequest request)
    {
        var item = await _inventoryService.RestockAsync(productId, request.Quantity);
        return Ok(item);
    }

    /// <summary>
    /// Returns inventory items where on-hand quantity is at or below the reorder level.
    /// </summary>
    /// <returns>200 OK with a list of low-stock inventory items.</returns>
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock() => Ok(await _inventoryService.GetLowStockItemsAsync());
}

/// <summary>Request body for restocking a product's inventory.</summary>
/// <param name="Quantity">The number of units to add to the current stock.</param>
public record RestockRequest(int Quantity);
