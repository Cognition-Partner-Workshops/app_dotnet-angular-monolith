using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Provides business logic for managing <see cref="Product"/> entities,
/// including catalog browsing, category filtering, and product creation.
/// </summary>
public class ProductService
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="ProductService"/>.
    /// </summary>
    /// <param name="context">The database context used for data access.</param>
    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all products with their associated inventory data.
    /// </summary>
    /// <returns>A list of all <see cref="Product"/> records with eagerly loaded inventory.</returns>
    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await _context.Products.Include(p => p.Inventory).ToListAsync();
    }

    /// <summary>
    /// Retrieves a single product by ID, including its inventory data.
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <returns>The matching <see cref="Product"/> with inventory, or <c>null</c> if not found.</returns>
    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _context.Products.Include(p => p.Inventory).FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>
    /// Creates a new product record in the database.
    /// </summary>
    /// <param name="product">The product entity to persist.</param>
    /// <returns>The created <see cref="Product"/> with its generated ID.</returns>
    public async Task<Product> CreateProductAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    /// <summary>
    /// Retrieves all products belonging to the specified category, with inventory data.
    /// </summary>
    /// <param name="category">The category name to filter by (exact match).</param>
    /// <returns>A list of matching <see cref="Product"/> records.</returns>
    public async Task<List<Product>> GetProductsByCategoryAsync(string category)
    {
        return await _context.Products.Where(p => p.Category == category).Include(p => p.Inventory).ToListAsync();
    }
}
