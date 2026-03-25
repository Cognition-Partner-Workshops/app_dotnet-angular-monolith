using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

/// <summary>
/// Proxies inventory requests to the inventory-service microservice via InventoryApiClient.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly InventoryApiClient _inventoryApiClient;

    public InventoryController(InventoryApiClient inventoryApiClient)
    {
        _inventoryApiClient = inventoryApiClient;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _inventoryApiClient.GetAllInventoryAsync());

    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var item = await _inventoryApiClient.GetInventoryByProductIdAsync(productId);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("product/{productId}/restock")]
    public async Task<IActionResult> Restock(int productId, [FromBody] RestockRequest request)
    {
        var item = await _inventoryApiClient.RestockAsync(productId, request.Quantity);
        return Ok(item);
    }

    [HttpPost("product/{productId}/deduct")]
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

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock() => Ok(await _inventoryApiClient.GetLowStockItemsAsync());

    [HttpGet("product/{productId}/check")]
    public async Task<IActionResult> CheckStock(int productId, [FromQuery] int quantity = 1)
    {
        var available = await _inventoryApiClient.CheckStockAsync(productId, quantity);
        return Ok(new { productId, quantity, available });
    }

    [HttpPost("product/{productId}/deduct")]
    public async Task<IActionResult> DeductStock(int productId, [FromBody] DeductRequest request)
    {
        try
        {
            var item = await _inventoryApiClient.DeductStockAsync(productId, request.Quantity);
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
