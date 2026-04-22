using Microsoft.AspNetCore.Mvc;
using InventoryService.Api.Services;

namespace InventoryService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly InventoryItemService _inventoryService;

    public InventoryController(InventoryItemService inventoryService)
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

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock() => Ok(await _inventoryService.GetLowStockItemsAsync());

    [HttpPost("product/{productId}/check-and-deduct")]
    public async Task<IActionResult> CheckAndDeduct(int productId, [FromBody] DeductRequest request)
    {
        var result = await _inventoryService.CheckAndDeductStockAsync(productId, request.Quantity);
        if (!result.Success)
            return Conflict(new { result.Success, result.RemainingStock, result.Error });
        return Ok(new { result.Success, result.RemainingStock });
    }

    [HttpGet("product/{productId}/stock")]
    public async Task<IActionResult> GetStockLevel(int productId)
    {
        var level = await _inventoryService.GetStockLevelAsync(productId);
        return level is null ? NotFound() : Ok(new { productId, quantityOnHand = level });
    }
}

public record RestockRequest(int Quantity);
public record DeductRequest(int Quantity);
