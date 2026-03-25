namespace OrderManager.Api.Services;

public interface IInventoryServiceClient
{
    Task<List<InventoryItemDto>> GetAllInventoryAsync();
    Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId);
    Task<List<InventoryItemDto>> GetLowStockItemsAsync();
    Task<InventoryItemDto?> RestockAsync(int productId, int quantity);
    Task<InventoryItemDto?> DeductStockAsync(int productId, int quantity);
}

public class InventoryItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime LastRestocked { get; set; }
}
