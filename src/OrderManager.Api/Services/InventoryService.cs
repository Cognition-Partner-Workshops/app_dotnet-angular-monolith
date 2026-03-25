using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Inventory service that delegates to the standalone inventory microservice via HTTP.
/// Maintains the same public API for backward compatibility with existing controllers.
/// </summary>
public class InventoryService
{
    private readonly InventoryServiceClient _client;

    public InventoryService(InventoryServiceClient client)
    {
        _client = client;
    }

    public async Task<List<InventoryItemDto>> GetAllInventoryAsync()
    {
        return await _client.GetAllInventoryAsync();
    }

    public async Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId)
    {
        return await _client.GetInventoryByProductIdAsync(productId);
    }

    public async Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        return await _client.RestockAsync(productId, quantity);
    }

    public async Task<List<InventoryItemDto>> GetLowStockItemsAsync()
    {
        return await _client.GetLowStockItemsAsync();
    }
}
