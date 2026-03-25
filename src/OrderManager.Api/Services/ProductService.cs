using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// CRUD and query operations for <see cref="Product"/> entities.
/// Products are always returned with their related <see cref="InventoryItem"/> included.
/// </summary>
public class ProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Returns all products with their current inventory levels.</summary>
    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await _context.Products.Include(p => p.Inventory).ToListAsync();
    }

    /// <summary>Returns a product by ID with inventory info, or null if not found.</summary>
    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _context.Products.Include(p => p.Inventory).FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>Persists a new product and returns it with the generated ID.</summary>
    public async Task<Product> CreateProductAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    /// <summary>Returns products matching the given category (case-sensitive).</summary>
    public async Task<List<Product>> GetProductsByCategoryAsync(string category)
    {
        return await _context.Products.Where(p => p.Category == category).Include(p => p.Inventory).ToListAsync();
    }
}
