// This model is no longer used in the monolith.
// Inventory data is now owned by the standalone inventory microservice.
// The InventoryServiceClient (Services/InventoryServiceClient.cs) communicates
// with the microservice via HTTP and uses InventoryItemDto for responses.
namespace OrderManager.Api.Models;

public class InventoryItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; } = 10;
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime LastRestocked { get; set; } = DateTime.UtcNow;
}
