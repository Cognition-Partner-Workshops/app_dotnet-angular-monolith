using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

public interface IProductService
{
    Task<List<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(int id);
    Task<Product> CreateProductAsync(Product product);
    Task<List<Product>> GetProductsByCategoryAsync(string category);
}
