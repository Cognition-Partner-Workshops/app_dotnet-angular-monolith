namespace OrderManager.Api.Models;

/// <summary>
/// DTO for inventory data received from the inventory microservice.
/// Replaces the former EF Core InventoryItem entity after decomposition.
/// </summary>
public class InventoryItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime LastRestocked { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
}
