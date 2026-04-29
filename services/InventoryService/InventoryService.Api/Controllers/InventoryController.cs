using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        try
        {
            var item = await _inventoryService.RestockAsync(productId, request.Quantity);
            return Ok(item);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Concurrent modification detected. Please retry." });
        }
    }

    [HttpPost("product/{productId}/reserve")]
    public async Task<IActionResult> Reserve(int productId, [FromBody] ReserveRequest request)
    {
        try
        {
            var (success, message) = await _inventoryService.ReserveStockAsync(productId, request.Quantity);
            if (!success)
                return BadRequest(new { success, message });
            return Ok(new { success, message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Concurrent modification detected. Please retry." });
        }
    }

    [HttpPost("product/{productId}/release")]
    public async Task<IActionResult> Release(int productId, [FromBody] ReleaseRequest request)
    {
        try
        {
            var (success, message) = await _inventoryService.ReleaseStockAsync(productId, request.Quantity);
            if (!success)
                return BadRequest(new { success, message });
            return Ok(new { success, message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Concurrent modification detected. Please retry." });
        }
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock() => Ok(await _inventoryService.GetLowStockItemsAsync());
}

public record RestockRequest(int Quantity);
public record ReserveRequest(int Quantity);
public record ReleaseRequest(int Quantity);
