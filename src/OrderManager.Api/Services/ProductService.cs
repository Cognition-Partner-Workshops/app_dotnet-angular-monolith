using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Provides business logic for managing the <see cref="Product"/> catalog.
/// </summary>
public class ProductService
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="ProductService"/>.
    /// </summary>
    /// <param name="context">The database context for data access.</param>
    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all products with their current inventory levels.
    /// </summary>
    /// <returns>A list of all <see cref="Product"/> records with <see cref="Product.Inventory"/> included.</returns>
    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await _context.Products.Include(p => p.Inventory).ToListAsync();
    }

    /// <summary>
    /// Retrieves a single product by ID with its inventory data.
    /// </summary>
    /// <param name="id">The product's unique identifier.</param>
    /// <returns>The matching <see cref="Product"/> with inventory included, or <c>null</c> if not found.</returns>
    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _context.Products.Include(p => p.Inventory).FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>
    /// Creates a new product and persists it to the database.
    /// </summary>
    /// <param name="product">The product entity to create.</param>
    /// <returns>The created <see cref="Product"/> with its generated ID.</returns>
    public async Task<Product> CreateProductAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    /// <summary>
    /// Retrieves all products belonging to the specified category.
    /// </summary>
    /// <param name="category">The category name to filter by (exact match).</param>
    /// <returns>A filtered list of <see cref="Product"/> records with inventory included.</returns>
    public async Task<List<Product>> GetProductsByCategoryAsync(string category)
    {
        return await _context.Products.Where(p => p.Category == category).Include(p => p.Inventory).ToListAsync();
    }
}
