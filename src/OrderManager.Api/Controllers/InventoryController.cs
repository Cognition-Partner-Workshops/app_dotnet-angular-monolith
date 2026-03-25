using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

/// <summary>
/// Inventory controller that proxies requests to the inventory microservice.
/// Maintains backward-compatible API surface for existing Angular frontend.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InventoryController : ControllerBase
{
    private readonly InventoryServiceClient _inventoryClient;

    /// <summary>
    /// Initializes a new instance of <see cref="InventoryController"/>.
    /// </summary>
    /// <param name="inventoryClient">The HTTP client for the inventory microservice.</param>
    public InventoryController(InventoryServiceClient inventoryClient)
    {
        _inventoryClient = inventoryClient;
    }

    /// <summary>Returns all inventory items.</summary>
    /// <response code="200">List of all inventory items.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<InventoryItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() => Ok(await _inventoryClient.GetAllInventoryAsync());

    /// <summary>Returns the inventory record for a specific product.</summary>
    /// <param name="productId">The product identifier.</param>
    /// <response code="200">The inventory item.</response>
    /// <response code="404">No inventory record exists for this product.</response>
    [HttpGet("product/{productId}")]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var item = await _inventoryClient.GetInventoryByProductIdAsync(productId);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Adds stock to a product's inventory.</summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="request">Restock details including quantity to add.</param>
    /// <response code="200">Updated inventory item after restocking.</response>
    [HttpPost("product/{productId}/restock")]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Restock(int productId, [FromBody] RestockRequest request)
    {
        var item = await _inventoryClient.RestockAsync(productId, request.Quantity);
        return Ok(item);
    }

    /// <summary>Returns inventory items that are at or below the reorder threshold.</summary>
    /// <response code="200">List of low-stock inventory items.</response>
    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(List<InventoryItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStock() => Ok(await _inventoryClient.GetLowStockItemsAsync());
}

/// <summary>Request payload for restocking an inventory item.</summary>
/// <param name="Quantity">The number of units to add to stock.</param>
public record RestockRequest(int Quantity);
