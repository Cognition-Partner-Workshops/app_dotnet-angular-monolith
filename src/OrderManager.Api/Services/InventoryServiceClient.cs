using System.Net.Http.Json;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// HTTP client for communicating with the standalone inventory microservice.
/// Replaces direct database access for inventory operations.
/// </summary>
public class InventoryServiceClient : IInventoryServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryServiceClient> _logger;

    public InventoryServiceClient(HttpClient httpClient, ILogger<InventoryServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<InventoryItem>> GetAllInventoryAsync()
    {
        try
        {
            var items = await _httpClient.GetFromJsonAsync<List<InventoryItem>>("api/inventory");
            return items ?? new List<InventoryItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all inventory from inventory-service");
            throw;
        }
    }

    public async Task<InventoryItem?> GetInventoryByProductIdAsync(int productId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/inventory/product/{productId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<InventoryItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get inventory for product {ProductId} from inventory-service", productId);
            throw;
        }
    }

    public async Task<InventoryItem> RestockAsync(int productId, int quantity)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/inventory/product/{productId}/restock",
                new { Quantity = quantity });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<InventoryItem>()
                ?? throw new InvalidOperationException("Restock returned null response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restock product {ProductId} via inventory-service", productId);
            throw;
        }
    }

    public async Task<List<InventoryItem>> GetLowStockItemsAsync()
    {
        try
        {
            var items = await _httpClient.GetFromJsonAsync<List<InventoryItem>>("api/inventory/low-stock");
            return items ?? new List<InventoryItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get low stock items from inventory-service");
            throw;
        }
    }

    public async Task<InventoryItem> DeductStockAsync(int productId, int quantity)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/inventory/product/{productId}/deduct",
                new { Quantity = quantity });
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                throw new InvalidOperationException($"Insufficient stock for product {productId}");
            }
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<InventoryItem>()
                ?? throw new InvalidOperationException($"Deduct returned null for product {productId}");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deduct stock for product {ProductId} via inventory-service", productId);
            throw;
        }
    }

    public async Task<StockReservationResponse> CheckAndReserveStockAsync(StockReservationRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/inventory/check-and-reserve", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<StockReservationResponse>()
                ?? throw new InvalidOperationException("Check-and-reserve returned null response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check and reserve stock via inventory-service");
            throw;
        }
    }
}


public class StockReservationRequest
{
    public List<StockReservationItem> Items { get; set; } = new();
}

public class StockReservationItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class StockReservationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ReservationDetail> Details { get; set; } = new();
}

public class ReservationDetail
{
    public int ProductId { get; set; }
    public int RequestedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public bool Reserved { get; set; }
}
