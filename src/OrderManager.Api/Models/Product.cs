namespace OrderManager.Api.Models;

/// <summary>
/// Represents a product available for purchase in the catalog.
/// </summary>
public class Product
{
    /// <summary>Gets or sets the unique identifier for the product.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the display name of the product.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a detailed description of the product.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the product category (e.g., "Widgets", "Gadgets").</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the unit price of the product. Stored as decimal(18,2).</summary>
    public decimal Price { get; set; }

    /// <summary>Gets or sets the stock-keeping unit code. Must be unique across all products.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when the product was added to the catalog.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the associated inventory record for this product (one-to-one).</summary>
    public InventoryItem? Inventory { get; set; }

    /// <summary>Gets or sets the collection of order line items that reference this product.</summary>
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
