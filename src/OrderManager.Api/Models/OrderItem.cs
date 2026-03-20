namespace OrderManager.Api.Models;

/// <summary>
/// Represents a single line item within an order, linking a product to a quantity and price.
/// </summary>
public class OrderItem
{
    /// <summary>Gets or sets the unique identifier for the order item.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the foreign key referencing the parent order.</summary>
    public int OrderId { get; set; }

    /// <summary>Gets or sets the navigation property to the parent order.</summary>
    public Order Order { get; set; } = null!;

    /// <summary>Gets or sets the foreign key referencing the ordered product.</summary>
    public int ProductId { get; set; }

    /// <summary>Gets or sets the navigation property to the ordered product.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>Gets or sets the number of units ordered.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets the price per unit at the time the order was placed.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Gets the computed line total (Quantity * UnitPrice).</summary>
    public decimal LineTotal => Quantity * UnitPrice;
}
