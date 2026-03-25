namespace OrderManager.Api.Models;

public class InventoryItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    // Product navigation removed — inventory is now managed by inventory-service microservice
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; } = 10;
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime LastRestocked { get; set; } = DateTime.UtcNow;
}
