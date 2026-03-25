namespace OrderManager.Api.Models;

/// <summary>
/// A single line item within an <see cref="Order"/>.
/// Captures the product, quantity, and the unit price at the time of purchase.
/// </summary>
public class OrderItem
{
    public int Id { get; set; }

    /// <summary>Foreign key to the parent <see cref="Order"/>.</summary>
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    /// <summary>Foreign key to the <see cref="Product"/> being ordered.</summary>
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>Number of units ordered.</summary>
    public int Quantity { get; set; }

    /// <summary>Price per unit captured at order time (may differ from current product price).</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Computed total for this line item (<see cref="Quantity"/> * <see cref="UnitPrice"/>).</summary>
    public decimal LineTotal => Quantity * UnitPrice;
}
