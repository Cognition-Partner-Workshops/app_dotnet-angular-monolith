using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Service responsible for managing product catalog operations.
/// Provides methods for querying products by various criteria and creating new products.
/// </summary>
public class ProductService
{
    /// <summary>
    /// The database context used for product data access operations.
    /// </summary>
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="ProductService"/> with the specified database context.
    /// </summary>
    /// <param name="context">The application database context for data access.</param>
    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all products from the database, including their associated inventory records.
    /// </summary>
    /// <returns>A list of all <see cref="Product"/> entities with eagerly loaded inventory data.</returns>
    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await _context.Products.Include(p => p.Inventory).ToListAsync();
    }

    /// <summary>
    /// Retrieves a single product by its unique identifier, including its inventory record.
    /// </summary>
    /// <param name="id">The unique identifier of the product to retrieve.</param>
    /// <returns>The <see cref="Product"/> with the specified ID and eagerly loaded inventory, or null if not found.</returns>
    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _context.Products.Include(p => p.Inventory).FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>
    /// Creates a new product record in the database.
    /// </summary>
    /// <param name="product">The product entity to create.</param>
    /// <returns>The created <see cref="Product"/> entity with its generated ID.</returns>
    public async Task<Product> CreateProductAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    /// <summary>
    /// Retrieves all products belonging to a specific category, including their inventory records.
    /// </summary>
    /// <param name="category">The category name to filter by (exact match).</param>
    /// <returns>A list of <see cref="Product"/> entities in the specified category.</returns>
    public async Task<List<Product>> GetProductsByCategoryAsync(string category)
    {
        return await _context.Products.Where(p => p.Category == category).Include(p => p.Inventory).ToListAsync();
    }
}
