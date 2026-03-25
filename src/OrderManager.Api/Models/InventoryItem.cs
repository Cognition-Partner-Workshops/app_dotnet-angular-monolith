namespace OrderManager.Api.Models;

/// <summary>
/// DTO representing an inventory record from the inventory microservice.
/// This is a lightweight model used for HTTP client deserialization only —
/// the monolith no longer owns the inventory table.
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
