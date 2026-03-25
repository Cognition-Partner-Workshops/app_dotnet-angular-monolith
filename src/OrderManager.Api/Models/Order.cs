namespace OrderManager.Api.Models;

/// <summary>
/// Represents a customer order containing one or more line items.
/// Status progresses through: Pending → Processing → Shipped → Delivered.
/// </summary>
public class Order
{
    public int Id { get; set; }

    /// <summary>Foreign key to the <see cref="Customer"/> who placed the order.</summary>
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>UTC timestamp recorded when the order is created.</summary>
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    /// <summary>Current fulfillment status (e.g. Pending, Processing, Shipped, Delivered).</summary>
    public string Status { get; set; } = "Pending";

    /// <summary>Sum of all line-item totals, stored as a decimal for currency precision.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Formatted shipping address derived from the customer's profile at order time.</summary>
    public string ShippingAddress { get; set; } = string.Empty;

    /// <summary>Line items belonging to this order.</summary>
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
