using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Proxies inventory operations to the standalone inventory-service via HTTP.
/// Replaces the former in-process EF Core implementation.
/// </summary>
public class InventoryService
{
    private readonly InventoryHttpClient _client;

    public InventoryService(InventoryHttpClient client)
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
