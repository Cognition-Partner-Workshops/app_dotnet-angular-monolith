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
    private readonly IInventoryServiceClient _inventoryClient;

    /// <summary>
    /// Initializes a new instance of <see cref="InventoryController"/>.
    /// </summary>
    /// <param name="inventoryClient">The HTTP client for the inventory microservice.</param>
    public InventoryController(IInventoryServiceClient inventoryClient)
    {
        _inventoryClient = inventoryClient;
    }

    /// <summary>Returns all inventory items.</summary>
    /// <response code="200">List of all inventory items.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() => Ok(await _inventoryClient.GetAllInventoryAsync());

    /// <summary>Returns the inventory record for a specific product.</summary>
    /// <param name="productId">The product identifier.</param>
    /// <response code="200">The inventory item.</response>
    /// <response code="404">No inventory record exists for this product.</response>
    [HttpGet("product/{productId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Restock(int productId, [FromBody] RestockRequest request)
    {
        var item = await _inventoryClient.RestockAsync(productId, request.Quantity);
        return Ok(item);
    }

    /// <summary>Deducts stock from a product's inventory.</summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="request">Deduction details including quantity to remove.</param>
    /// <response code="200">Stock was successfully deducted.</response>
    /// <response code="404">No inventory record exists for this product.</response>
    /// <response code="409">Insufficient stock to fulfil the deduction.</response>
    [HttpPost("product/{productId}/deduct")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deduct(int productId, [FromBody] DeductRequest request)
    {
        try
        {
            var item = await _inventoryClient.DeductStockAsync(productId, request.Quantity);
            return Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Returns inventory items that are at or below the reorder threshold.</summary>
    /// <response code="200">List of low-stock inventory items.</response>
    [HttpGet("low-stock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStock() => Ok(await _inventoryClient.GetLowStockItemsAsync());
}

/// <summary>Request payload for restocking an inventory item.</summary>
/// <param name="Quantity">The number of units to add to stock.</param>
public record RestockRequest(int Quantity);

/// <summary>Request payload for deducting stock from an inventory item.</summary>
/// <param name="Quantity">The number of units to deduct.</param>
public record DeductRequest(int Quantity);
