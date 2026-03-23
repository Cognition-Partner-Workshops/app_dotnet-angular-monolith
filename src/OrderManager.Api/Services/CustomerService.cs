using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Service responsible for managing customer operations in the OrderManager system.
/// Provides methods for querying and creating customer records via Entity Framework Core.
/// </summary>
public class CustomerService
{
    /// <summary>
    /// The database context used for customer data access operations.
    /// </summary>
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="CustomerService"/> with the specified database context.
    /// </summary>
    /// <param name="context">The application database context for data access.</param>
    public CustomerService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all customers from the database.
    /// </summary>
    /// <returns>A list of all <see cref="Customer"/> entities.</returns>
    public async Task<List<Customer>> GetAllCustomersAsync()
    {
        return await _context.Customers.ToListAsync();
    }

    /// <summary>
    /// Retrieves a single customer by their unique identifier, including their associated orders.
    /// </summary>
    /// <param name="id">The unique identifier of the customer to retrieve.</param>
    /// <returns>The <see cref="Customer"/> with the specified ID and eagerly loaded orders, or null if not found.</returns>
    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        return await _context.Customers.Include(c => c.Orders).FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <summary>
    /// Creates a new customer record in the database.
    /// </summary>
    /// <param name="customer">The customer entity to create.</param>
    /// <returns>The created <see cref="Customer"/> entity with its generated ID.</returns>
    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }
}
