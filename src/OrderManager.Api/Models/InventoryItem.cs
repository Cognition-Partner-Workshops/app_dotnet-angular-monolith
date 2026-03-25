using System.Text.Json.Serialization;

namespace OrderManager.Api.Models;

/// <summary>
/// DTO for deserializing inventory data from the inventory microservice.
/// This is no longer an EF Core entity — the inventory bounded context
/// is now owned by the standalone inventory-service.
/// </summary>
public class InventoryItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    [JsonIgnore]
    public Product? Product { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime? LastRestocked { get; set; }
}
