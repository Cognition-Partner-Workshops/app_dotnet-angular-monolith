using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly InventoryApiClient _inventoryClient;

    public InventoryController(InventoryApiClient inventoryClient)
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

    [HttpPost("product/{productId}/deduct")]
    public async Task<IActionResult> Deduct(int productId, [FromBody] DeductRequest request)
    {
        var success = await _inventoryClient.CheckAndDeductStockAsync(productId, request.Quantity);
        return success ? Ok() : Conflict(new { error = "Insufficient stock" });
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock() => Ok(await _inventoryClient.GetLowStockItemsAsync());
}

public record RestockRequest(int Quantity);
public record DeductRequest(int Quantity);
