namespace OrderManager.Api.Services;

/// <summary>
/// Refactored InventoryService that delegates to the standalone inventory-service via HTTP.
/// Previously accessed the database directly through AppDbContext.
/// </summary>
public class InventoryService
{
    private readonly InventoryHttpClient _inventoryClient;

    public InventoryService(InventoryHttpClient inventoryClient)
    {
        _inventoryClient = inventoryClient;
    }

    public async Task<List<InventoryItemDto>> GetAllInventoryAsync()
    {
        return await _inventoryClient.GetAllInventoryAsync();
    }

    public async Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId)
    {
        return await _inventoryClient.GetInventoryByProductIdAsync(productId);
    }

    public async Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        return await _inventoryClient.RestockAsync(productId, quantity);
    }

    public async Task DeductStockAsync(int productId, int quantity)
    {
        await _inventoryClient.DeductStockAsync(productId, quantity);
    }

    public async Task<List<InventoryItemDto>> GetLowStockItemsAsync()
    {
        return await _inventoryClient.GetLowStockItemsAsync();
    }

    public async Task<bool> CheckStockAsync(int productId, int quantity)
    {
        return await _inventoryClient.CheckStockAsync(productId, quantity);
    }
}
