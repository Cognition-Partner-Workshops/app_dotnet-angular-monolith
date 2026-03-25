using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

public class InventoryService
{
    private readonly InventoryHttpClient _inventoryClient;

    public InventoryService(InventoryHttpClient inventoryClient)
    {
        _inventoryClient = inventoryClient;
    }

    public async Task<List<InventoryItem>> GetAllInventoryAsync()
    {
        return await _inventoryClient.GetAllInventoryAsync();
    }

    public async Task<InventoryItem?> GetInventoryByProductIdAsync(int productId)
    {
        return await _inventoryClient.GetInventoryByProductIdAsync(productId);
    }

    public async Task<InventoryItem> RestockAsync(int productId, int quantity)
    {
        return await _inventoryClient.RestockAsync(productId, quantity);
    }

    public async Task<List<InventoryItem>> GetLowStockItemsAsync()
    {
        return await _inventoryClient.GetLowStockItemsAsync();
    }

    public async Task<bool> CheckStockAsync(int productId, int quantity)
    {
        return await _inventoryClient.CheckStockAsync(productId, quantity);
    }

    public async Task<InventoryItem?> DeductStockAsync(int productId, int quantity)
    {
        return await _inventoryClient.DeductStockAsync(productId, quantity);
    }
}
