namespace OrderManager.Api.Models;

/// <summary>
/// Represents a customer who can place orders.
/// Email is enforced as unique at the database level.
/// </summary>
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Unique email address — used as the business identifier for the customer.</summary>
    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    // --- Mailing / shipping address fields ---------------------------------
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;

    /// <summary>UTC timestamp recorded when the customer record is first created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation collection of all orders placed by this customer.</summary>
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
