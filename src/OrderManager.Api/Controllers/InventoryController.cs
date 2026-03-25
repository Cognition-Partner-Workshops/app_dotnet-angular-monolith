using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

/// <summary>
/// REST endpoints for warehouse inventory management.
/// Supports stock queries, restocking, and low-stock alerts.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly InventoryService _inventoryService;

    public InventoryController(InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>GET /api/inventory — returns all inventory items with product details.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _inventoryService.GetAllInventoryAsync());

    /// <summary>GET /api/inventory/product/{productId} — returns inventory for a single product or 404.</summary>
    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var item = await _inventoryService.GetInventoryByProductIdAsync(productId);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>POST /api/inventory/product/{productId}/restock — adds units to on-hand stock.</summary>
    [HttpPost("product/{productId}/restock")]
    public async Task<IActionResult> Restock(int productId, [FromBody] RestockRequest request)
    {
        var item = await _inventoryService.RestockAsync(productId, request.Quantity);
        return Ok(item);
    }

    /// <summary>GET /api/inventory/low-stock — returns items at or below their reorder level.</summary>
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock() => Ok(await _inventoryService.GetLowStockItemsAsync());
}

/// <summary>Request body for restocking an inventory item.</summary>
public record RestockRequest(int Quantity);
