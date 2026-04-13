using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Provides business logic for managing product inventory, including stock queries and restocking.
/// </summary>
public class InventoryService
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="InventoryService"/>.
    /// </summary>
    /// <param name="context">The database context used for inventory data access.</param>
    public InventoryService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all inventory items, including their associated product details.
    /// </summary>
    /// <returns>A list of all <see cref="InventoryItem"/> records with product data eagerly loaded.</returns>
    public async Task<List<InventoryItem>> GetAllInventoryAsync()
    {
        return await _context.InventoryItems.Include(i => i.Product).ToListAsync();
    }

    /// <summary>
    /// Retrieves the inventory record for a specific product.
    /// </summary>
    /// <param name="productId">The identifier of the product to look up.</param>
    /// <returns>The matching <see cref="InventoryItem"/> with product loaded, or <c>null</c> if not found.</returns>
    public async Task<InventoryItem?> GetInventoryByProductIdAsync(int productId)
    {
        return await _context.InventoryItems.Include(i => i.Product).FirstOrDefaultAsync(i => i.ProductId == productId);
    }

    /// <summary>
    /// Adds the specified quantity to a product's on-hand stock and updates the restock timestamp.
    /// </summary>
    /// <param name="productId">The identifier of the product to restock.</param>
    /// <param name="quantity">The number of units to add to the current stock.</param>
    /// <returns>The updated <see cref="InventoryItem"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when no inventory record exists for the given product.</exception>
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
    /// Retrieves all inventory items whose on-hand quantity is at or below their reorder level.
    /// </summary>
    /// <returns>A list of <see cref="InventoryItem"/> records that need restocking.</returns>
    public async Task<List<InventoryItem>> GetLowStockItemsAsync()
    {
        return await _context.InventoryItems
            .Include(i => i.Product)
            .Where(i => i.QuantityOnHand <= i.ReorderLevel)
            .ToListAsync();
    }
}
