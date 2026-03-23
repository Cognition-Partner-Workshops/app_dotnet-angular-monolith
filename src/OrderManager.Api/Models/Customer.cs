namespace OrderManager.Api.Models;

/// <summary>
/// Represents a customer entity in the OrderManager system.
/// Customers can place orders and have associated contact and address information.
/// </summary>
public class Customer
{
    /// <summary>
    /// Gets or sets the unique identifier for the customer.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the full name of the customer or organization.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer's email address. Must be unique across all customers.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer's phone number.
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the street address of the customer.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the city portion of the customer's address.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state or province of the customer's address.
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ZIP or postal code of the customer's address.
    /// </summary>
    public string ZipCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when the customer record was created.
    /// Defaults to the current UTC time on creation.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the collection of orders placed by this customer.
    /// Navigation property for the one-to-many relationship with <see cref="Order"/>.
    /// </summary>
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
