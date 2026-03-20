using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

/// <summary>
/// API controller for inventory management operations.
/// Exposes endpoints under <c>/api/inventory</c>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly InventoryService _inventoryService;

    /// <summary>
    /// Initializes the controller with the required inventory service.
    /// </summary>
    /// <param name="inventoryService">The service handling inventory business logic.</param>
    public InventoryController(InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// Retrieves all inventory items with their associated product information.
    /// </summary>
    /// <returns>200 OK with a list of all inventory items.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _inventoryService.GetAllInventoryAsync());

    /// <summary>
    /// Retrieves the inventory record for a specific product.
    /// </summary>
    /// <param name="productId">The product ID to look up.</param>
    /// <returns>200 OK with the inventory item, or 404 Not Found.</returns>
    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var item = await _inventoryService.GetInventoryByProductIdAsync(productId);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>
    /// Restocks a product by adding the specified quantity to its current on-hand stock.
    /// </summary>
    /// <param name="productId">The product ID to restock.</param>
    /// <param name="request">The restock payload containing the quantity to add.</param>
    /// <returns>200 OK with the updated inventory item.</returns>
    [HttpPost("product/{productId}/restock")]
    public async Task<IActionResult> Restock(int productId, [FromBody] RestockRequest request)
    {
        var item = await _inventoryService.RestockAsync(productId, request.Quantity);
        return Ok(item);
    }

    /// <summary>
    /// Retrieves all inventory items that are at or below their reorder level.
    /// </summary>
    /// <returns>200 OK with a list of low-stock inventory items.</returns>
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock() => Ok(await _inventoryService.GetLowStockItemsAsync());
}

/// <summary>
/// Request payload for restocking a product's inventory.
/// </summary>
/// <param name="Quantity">The number of units to add to current stock.</param>
public record RestockRequest(int Quantity);
