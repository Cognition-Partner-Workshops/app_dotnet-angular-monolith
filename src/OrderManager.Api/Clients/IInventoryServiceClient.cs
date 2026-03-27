namespace OrderManager.Api.Clients;

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

public class StockCheckResult
{
    public int ProductId { get; set; }
    public int RequestedQuantity { get; set; }
    public bool Available { get; set; }
}

public interface IInventoryServiceClient
{
    Task<List<InventoryItemDto>> GetAllInventoryAsync();
    Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId);
    Task<InventoryItemDto> RestockAsync(int productId, int quantity);
    Task<List<InventoryItemDto>> GetLowStockItemsAsync();
    Task<bool> CheckStockAsync(int productId, int quantity);
    Task<InventoryItemDto?> DeductStockAsync(int productId, int quantity);
}
