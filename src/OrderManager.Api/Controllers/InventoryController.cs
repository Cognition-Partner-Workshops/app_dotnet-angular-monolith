using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly InventoryServiceClient _inventoryServiceClient;

    public InventoryController(InventoryServiceClient inventoryServiceClient)
    {
        _inventoryServiceClient = inventoryServiceClient;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _inventoryServiceClient.GetAllInventoryAsync());

    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var item = await _inventoryServiceClient.GetInventoryByProductIdAsync(productId);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("product/{productId}/restock")]
    public async Task<IActionResult> Restock(int productId, [FromBody] RestockRequest request)
    {
        var item = await _inventoryServiceClient.RestockAsync(productId, request.Quantity);
        return Ok(item);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock() => Ok(await _inventoryServiceClient.GetLowStockItemsAsync());
}

public record RestockRequest(int Quantity);
