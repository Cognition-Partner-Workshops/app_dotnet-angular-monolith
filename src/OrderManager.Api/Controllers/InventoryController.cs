using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Clients;

namespace OrderManager.Api.Controllers;

/// <summary>
/// Proxies inventory requests to the inventory-service microservice via InventoryService HTTP client.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryClient _inventoryClient;

    public InventoryController(IInventoryClient inventoryClient)
    {
        _inventoryClient = inventoryClient;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _inventoryClient.GetAllInventoryAsync());

    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var item = await _inventoryClient.GetInventoryByProductIdAsync(productId);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("product/{productId}/restock")]
    public async Task<IActionResult> Restock(int productId, [FromBody] RestockRequest request)
    {
        var item = await _inventoryClient.RestockAsync(productId, request.Quantity);
        return Ok(item);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock() => Ok(await _inventoryClient.GetLowStockItemsAsync());

    [HttpGet("product/{productId}/check")]
    public async Task<IActionResult> CheckStock(int productId, [FromQuery] int quantity = 1)
    {
        var available = await _inventoryClient.CheckStockAsync(productId, quantity);
        return Ok(new { productId, quantity, available });
    }

    [HttpPost("product/{productId}/deduct")]
    public async Task<IActionResult> DeductStock(int productId, [FromBody] DeductRequest request)
    {
        try
        {
            var item = await _inventoryClient.DeductStockAsync(productId, request.Quantity);
            return Ok(item);
        }
        catch (HttpRequestException)
        {
            return StatusCode(502, new { error = "Inventory service unavailable" });
        }
    }
}

public record RestockRequest(int Quantity);
public record DeductRequest(int Quantity);
