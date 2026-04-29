using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly Services.InventoryService _inventoryService;

    public InventoryController(Services.InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _inventoryService.GetAllInventoryAsync());

    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var item = await _inventoryService.GetInventoryByProductIdAsync(productId);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("product/{productId}/restock")]
    public async Task<IActionResult> Restock(int productId, [FromBody] RestockRequest request)
    {
        var item = await _inventoryService.RestockAsync(productId, request.Quantity);
        return Ok(item);
    }

    [HttpPost("product/{productId}/reserve")]
    public async Task<IActionResult> Reserve(int productId, [FromBody] ReserveRequest request)
    {
        var (success, message) = await _inventoryService.ReserveStockAsync(productId, request.Quantity);
        if (!success)
            return BadRequest(new { success, message });
        return Ok(new { success, message });
    }

    [HttpPost("product/{productId}/release")]
    public async Task<IActionResult> Release(int productId, [FromBody] ReleaseRequest request)
    {
        var (success, message) = await _inventoryService.ReleaseStockAsync(productId, request.Quantity);
        if (!success)
            return BadRequest(new { success, message });
        return Ok(new { success, message });
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock() => Ok(await _inventoryService.GetLowStockItemsAsync());
}

public record RestockRequest(int Quantity);
public record ReserveRequest(int Quantity);
public record ReleaseRequest(int Quantity);
