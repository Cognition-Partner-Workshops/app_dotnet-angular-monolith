using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Manages warehouse inventory levels for products.
/// Provides restocking and low-stock alerting capabilities.
/// </summary>
public class InventoryService
{
    private readonly AppDbContext _context;

    public InventoryService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Returns all inventory records with their associated product details.</summary>
    public async Task<List<InventoryItem>> GetAllInventoryAsync()
    {
        return await _context.InventoryItems.Include(i => i.Product).ToListAsync();
    }

    /// <summary>Returns the inventory record for a specific product, or null if none exists.</summary>
    public async Task<InventoryItem?> GetInventoryByProductIdAsync(int productId)
    {
        return await _context.InventoryItems.Include(i => i.Product).FirstOrDefaultAsync(i => i.ProductId == productId);
    }

    /// <summary>
    /// Adds <paramref name="quantity"/> units to the product's on-hand count
    /// and records the current UTC time as the last-restocked timestamp.
    /// </summary>
    /// <exception cref="ArgumentException">No inventory record exists for the product.</exception>
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
    /// Returns inventory items whose on-hand quantity is at or below their reorder level.
    /// Used by the dashboard to highlight items needing replenishment.
    /// </summary>
    public async Task<List<InventoryItem>> GetLowStockItemsAsync()
    {
        return await _context.InventoryItems
            .Include(i => i.Product)
            .Where(i => i.QuantityOnHand <= i.ReorderLevel)
            .ToListAsync();
    }
}
