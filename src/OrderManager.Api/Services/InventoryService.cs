using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Service responsible for managing warehouse inventory operations.
/// Provides methods for querying stock levels, restocking products, and identifying low-stock items.
/// </summary>
public class InventoryService
{
    /// <summary>
    /// The database context used for inventory data access operations.
    /// </summary>
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="InventoryService"/> with the specified database context.
    /// </summary>
    /// <param name="context">The application database context for data access.</param>
    public InventoryService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all inventory items from the database, including their associated product details.
    /// </summary>
    /// <returns>A list of all <see cref="InventoryItem"/> entities with eagerly loaded products.</returns>
    public async Task<List<InventoryItem>> GetAllInventoryAsync()
    {
        return await _context.InventoryItems.Include(i => i.Product).ToListAsync();
    }

    /// <summary>
    /// Retrieves the inventory record for a specific product, including product details.
    /// </summary>
    /// <param name="productId">The unique identifier of the product whose inventory to retrieve.</param>
    /// <returns>The <see cref="InventoryItem"/> for the specified product, or null if no inventory record exists.</returns>
    public async Task<InventoryItem?> GetInventoryByProductIdAsync(int productId)
    {
        return await _context.InventoryItems.Include(i => i.Product).FirstOrDefaultAsync(i => i.ProductId == productId);
    }

    /// <summary>
    /// Restocks a product by adding the specified quantity to its current stock level.
    /// Updates the <see cref="InventoryItem.LastRestocked"/> timestamp to the current UTC time.
    /// </summary>
    /// <param name="productId">The unique identifier of the product to restock.</param>
    /// <param name="quantity">The number of units to add to the current stock.</param>
    /// <returns>The updated <see cref="InventoryItem"/> with the new stock level.</returns>
    /// <exception cref="ArgumentException">Thrown when no inventory record exists for the specified product.</exception>
    public async Task<InventoryItem> RestockAsync(int productId, int quantity)
    {
        var item = await _context.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId)
            ?? throw new ArgumentException($"No inventory record for product {productId}");
        item.QuantityOnHand += quantity;
        item.LastRestocked = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return item;
    }

    /// <summary>
    /// Retrieves all inventory items where the current stock level is at or below the reorder threshold.
    /// These items are flagged for replenishment.
    /// </summary>
    /// <returns>A list of <see cref="InventoryItem"/> entities that are at or below their reorder level.</returns>
    public async Task<List<InventoryItem>> GetLowStockItemsAsync()
    {
        return await _context.InventoryItems
            .Include(i => i.Product)
            .Where(i => i.QuantityOnHand <= i.ReorderLevel)
            .ToListAsync();
    }
}
