namespace OrderManager.Api.Models;

/// <summary>
/// DTO for inventory data returned by the inventory microservice.
/// No longer an EF entity — inventory is owned by the inventory-service.
/// </summary>
public class InventoryItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; } = 10;
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime LastRestocked { get; set; } = DateTime.UtcNow;
}
