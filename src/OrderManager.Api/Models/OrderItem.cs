namespace OrderManager.Api.Models;

/// <summary>
/// Represents a single line item within an order, linking to a specific product and quantity.
/// </summary>
public class OrderItem
{
    /// <summary>Gets or sets the unique identifier for this order item.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the foreign key to the parent order.</summary>
    public int OrderId { get; set; }

    /// <summary>Gets or sets the navigation property to the parent order.</summary>
    public Order Order { get; set; } = null!;

    /// <summary>Gets or sets the foreign key to the product being ordered.</summary>
    public int ProductId { get; set; }

    /// <summary>Gets or sets the navigation property to the product being ordered.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>Gets or sets the number of units ordered for this line item.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets the price per unit at the time the order was placed.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Gets the computed total for this line item (Quantity * UnitPrice).</summary>
    public decimal LineTotal => Quantity * UnitPrice;
}
