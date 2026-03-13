namespace OrderManager.Api.Models;

// DECOMPOSITION NOTE: The Customers domain has been extracted to a dedicated microservice.
// See: https://github.com/Cognition-Partner-Workshops/app_dotnet-angular-microservices
// The Orders navigation property below is retained because the Orders module still references it.
// In the extracted microservice, this property has been removed.
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
