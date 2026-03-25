using System.Net.Http.Json;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// HTTP client for communicating with the standalone inventory microservice.
/// Replaces direct database access for inventory operations.
/// </summary>
public class InventoryServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryServiceClient> _logger;

    public InventoryServiceClient(HttpClient httpClient, ILogger<InventoryServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<InventoryItemDto>> GetAllInventoryAsync()
    {
        try
        {
            var items = await _httpClient.GetFromJsonAsync<List<InventoryItemDto>>("api/inventory");
            return items ?? new List<InventoryItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all inventory from inventory-service");
            throw;
        }
    }

    public async Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/inventory/product/{productId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<InventoryItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get inventory for product {ProductId} from inventory-service", productId);
            throw;
        }
    }

    public async Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/inventory/product/{productId}/restock",
                new { Quantity = quantity });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<InventoryItemDto>()
                ?? throw new InvalidOperationException("Restock returned null response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restock product {ProductId} via inventory-service", productId);
            throw;
        }
    }

    public async Task<List<InventoryItemDto>> GetLowStockItemsAsync()
    {
        try
        {
            var items = await _httpClient.GetFromJsonAsync<List<InventoryItemDto>>("api/inventory/low-stock");
            return items ?? new List<InventoryItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get low stock items from inventory-service");
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

/// <summary>
/// DTO for inventory items returned from the inventory microservice.
/// Decoupled from EF Core entity to avoid direct database dependency.
/// </summary>
public class InventoryItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime LastRestocked { get; set; }
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
