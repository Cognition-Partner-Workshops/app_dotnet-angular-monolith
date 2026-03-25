namespace OrderManager.Api.Services;

public interface IInventoryServiceClient
{
    Task<InventoryCheckResult?> GetInventoryByProductIdAsync(int productId);
    Task<List<InventoryCheckResult>> GetAllInventoryAsync();
    Task<List<InventoryCheckResult>> GetLowStockItemsAsync();
    Task<InventoryCheckResult?> RestockAsync(int productId, int quantity);
    Task<bool> CheckStockAsync(int productId, int quantity);
    Task<InventoryCheckResult?> DeductStockAsync(int productId, int quantity);
}

public class InventoryCheckResult
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime LastRestocked { get; set; }
}

public class StockCheckResponse
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public bool Available { get; set; }
}
