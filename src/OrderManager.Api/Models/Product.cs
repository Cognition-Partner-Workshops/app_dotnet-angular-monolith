namespace OrderManager.Api.Models;

/// <summary>
/// Represents a product available for sale in the catalog.
/// </summary>
public class Product
{
    /// <summary>Gets or sets the unique identifier for the product.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the display name of the product.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a brief description of the product.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the product category used for filtering and grouping.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the unit price of the product in decimal currency.</summary>
    public decimal Price { get; set; }

    /// <summary>Gets or sets the stock-keeping unit code. Must be unique across all products.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when the product was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the associated inventory record for this product, if one exists.</summary>
    public InventoryItem? Inventory { get; set; }

    /// <summary>Gets or sets the collection of order line items referencing this product.</summary>
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
