using System.Net.Http.Json;

namespace OrderService.Api.Clients;

public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}

public class CustomerAddressDto
{
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
}

public interface ICustomerApiClient
{
    Task<CustomerDto?> GetCustomerAsync(int customerId);
    Task<CustomerAddressDto?> GetCustomerAddressAsync(int customerId);
}

public class CustomerApiClient : ICustomerApiClient
{
    private readonly HttpClient _httpClient;

    public CustomerApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CustomerDto?> GetCustomerAsync(int customerId)
    {
        var response = await _httpClient.GetAsync($"/api/customers/{customerId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomerDto>();
    }

    public async Task<CustomerAddressDto?> GetCustomerAddressAsync(int customerId)
    {
        var response = await _httpClient.GetAsync($"/api/customers/{customerId}/address");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomerAddressDto>();
    }
}
