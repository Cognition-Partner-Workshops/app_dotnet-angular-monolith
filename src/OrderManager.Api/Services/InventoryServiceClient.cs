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

    /// <summary>
    /// Initializes a new instance of <see cref="InventoryServiceClient"/>.
    /// </summary>
    /// <param name="httpClient">Pre-configured <see cref="HttpClient"/> targeting the inventory-service base URL.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public InventoryServiceClient(HttpClient httpClient, ILogger<InventoryServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>Retrieves all inventory items from the inventory microservice.</summary>
    /// <returns>A list of all inventory items, or an empty list if the service returns no data.</returns>
    /// <exception cref="HttpRequestException">Thrown when the inventory service is unreachable.</exception>
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

    /// <summary>Retrieves the inventory record for a specific product.</summary>
    /// <param name="productId">The product identifier.</param>
    /// <returns>The inventory item, or <c>null</c> if no record exists for the given product.</returns>
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

    /// <summary>Restocks an inventory item by adding the specified quantity.</summary>
    /// <param name="productId">The product identifier to restock.</param>
    /// <param name="quantity">The number of units to add.</param>
    /// <returns>The updated inventory item after restocking.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service returns a null response.</exception>
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

    /// <summary>Retrieves inventory items whose quantity is at or below the reorder threshold.</summary>
    /// <returns>A list of low-stock inventory items.</returns>
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

    /// <summary>Atomically checks stock availability and reserves units for the given items.</summary>
    /// <param name="request">The reservation request containing product/quantity pairs.</param>
    /// <returns>A response indicating whether the reservation succeeded and per-item details.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service returns a null response.</exception>
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
    /// <summary>Unique identifier for the inventory record.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key referencing the product in the catalog.</summary>
    public int ProductId { get; set; }

    /// <summary>Denormalized product name for display without cross-service calls.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Stock-keeping unit code.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>Current quantity available in the warehouse.</summary>
    public int QuantityOnHand { get; set; }

    /// <summary>Threshold below which the item is flagged as low stock.</summary>
    public int ReorderLevel { get; set; }

    /// <summary>Physical warehouse location code (e.g., "A-01").</summary>
    public string WarehouseLocation { get; set; } = string.Empty;

    /// <summary>UTC timestamp of the most recent restock operation.</summary>
    public DateTime LastRestocked { get; set; }
}

/// <summary>Request payload for the batch stock reservation endpoint.</summary>
public class StockReservationRequest
{
    /// <summary>The list of product/quantity pairs to reserve.</summary>
    public List<StockReservationItem> Items { get; set; } = new();
}

/// <summary>A single line item in a stock reservation request.</summary>
public class StockReservationItem
{
    /// <summary>The product identifier to reserve stock for.</summary>
    public int ProductId { get; set; }

    /// <summary>The number of units to reserve.</summary>
    public int Quantity { get; set; }
}

/// <summary>Response from the batch stock reservation endpoint.</summary>
public class StockReservationResponse
{
    /// <summary>Whether all requested items were successfully reserved.</summary>
    public bool Success { get; set; }

    /// <summary>A human-readable summary of the reservation outcome.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Per-item reservation details.</summary>
    public List<ReservationDetail> Details { get; set; } = new();
}

/// <summary>Per-item detail within a stock reservation response.</summary>
public class ReservationDetail
{
    /// <summary>The product identifier.</summary>
    public int ProductId { get; set; }

    /// <summary>The quantity that was requested.</summary>
    public int RequestedQuantity { get; set; }

    /// <summary>The quantity that was actually available at reservation time.</summary>
    public int AvailableQuantity { get; set; }

    /// <summary>Whether this specific item was successfully reserved.</summary>
    public bool Reserved { get; set; }
}
