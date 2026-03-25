namespace OrderManager.Api.Models;

/// <summary>
/// DTO representing an inventory record returned by the inventory-service microservice.
/// This is no longer an EF Core entity — the monolith consumes inventory data via HTTP.
/// </summary>
public class InventoryItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime LastRestocked { get; set; }
}
