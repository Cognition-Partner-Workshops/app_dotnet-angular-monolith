using System.Net.Http.Json;

namespace OrderService.Api.Clients;

public class ReserveStockResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ReleaseStockResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public interface IInventoryApiClient
{
    Task<ReserveStockResponse> ReserveStockAsync(int productId, int quantity);
    Task<ReleaseStockResponse> ReleaseStockAsync(int productId, int quantity);
}

public class InventoryApiClient : IInventoryApiClient
{
    private readonly HttpClient _httpClient;

    public InventoryApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ReserveStockResponse> ReserveStockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/inventory/product/{productId}/reserve",
            new { Quantity = quantity });

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var errorResult = await response.Content.ReadFromJsonAsync<ReserveStockResponse>();
            return errorResult ?? new ReserveStockResponse { Success = false, Message = "Reserve failed" };
        }

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ReserveStockResponse>();
        return result ?? new ReserveStockResponse { Success = false, Message = "Invalid response" };
    }

    public async Task<ReleaseStockResponse> ReleaseStockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/inventory/product/{productId}/release",
            new { Quantity = quantity });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ReleaseStockResponse>();
        return result ?? new ReleaseStockResponse { Success = false, Message = "Invalid response" };
    }
}
