using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Provides business logic for managing the product catalog.
/// </summary>
public class ProductService
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="ProductService"/>.
    /// </summary>
    /// <param name="context">The database context used for product data access.</param>
    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all products, including their inventory records.
    /// </summary>
    /// <returns>A list of all <see cref="Product"/> records with inventory data eagerly loaded.</returns>
    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await _context.Products.Include(p => p.Inventory).ToListAsync();
    }

    /// <summary>
    /// Retrieves a single product by its identifier, including inventory data.
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <returns>The matching <see cref="Product"/> with inventory loaded, or <c>null</c> if not found.</returns>
    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _context.Products.Include(p => p.Inventory).FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>
    /// Creates a new product record and persists it to the database.
    /// </summary>
    /// <param name="product">The product entity to create.</param>
    /// <returns>The newly created <see cref="Product"/> with its generated identifier.</returns>
    public async Task<Product> CreateProductAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    /// <summary>
    /// Retrieves all products belonging to the specified category, including their inventory records.
    /// </summary>
    /// <param name="category">The category name to filter by (case-sensitive).</param>
    /// <returns>A list of <see cref="Product"/> records matching the category.</returns>
    public async Task<List<Product>> GetProductsByCategoryAsync(string category)
    {
        return await _context.Products.Where(p => p.Category == category).Include(p => p.Inventory).ToListAsync();
    }
}
