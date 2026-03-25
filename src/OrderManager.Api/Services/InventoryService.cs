using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Local DB-backed inventory service — kept as a fallback.
/// In production the monolith delegates to the inventory microservice
/// via <see cref="InventoryServiceHttpClient"/>.
/// </summary>
public class InventoryService : IInventoryServiceClient
{
    private readonly AppDbContext _context;

    public InventoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<InventoryItem>> GetAllInventoryAsync()
    {
        return await _context.InventoryItems.Include(i => i.Product).ToListAsync();
    }

    public async Task<InventoryItem?> GetInventoryByProductIdAsync(int productId)
    {
        return await _context.InventoryItems.Include(i => i.Product).FirstOrDefaultAsync(i => i.ProductId == productId);
    }

    public async Task<InventoryItem> RestockAsync(int productId, int quantity)
    {
        var item = await _context.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId)
            ?? throw new ArgumentException($"No inventory record for product {productId}");
        item.QuantityOnHand += quantity;
        item.LastRestocked = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<InventoryItem> DeductStockAsync(int productId, int quantity)
    {
        var item = await _context.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId)
            ?? throw new ArgumentException($"No inventory record for product {productId}");

        if (item.QuantityOnHand < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}. Available: {item.QuantityOnHand}");

        item.QuantityOnHand -= quantity;
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<List<InventoryItem>> GetLowStockItemsAsync()
    {
        return await _context.InventoryItems
            .Include(i => i.Product)
            .Where(i => i.QuantityOnHand <= i.ReorderLevel)
            .ToListAsync();
    }
}
