namespace OrderManager.Api.Models;

public class InventoryItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; } = 10;
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime LastRestocked { get; set; } = DateTime.UtcNow;
}
