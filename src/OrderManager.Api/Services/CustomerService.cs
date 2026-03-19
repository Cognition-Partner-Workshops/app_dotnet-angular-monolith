using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Provides business logic for managing <see cref="Customer"/> entities.
/// </summary>
public class CustomerService
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="CustomerService"/>.
    /// </summary>
    /// <param name="context">The database context for data access.</param>
    public CustomerService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all customers without their related orders.
    /// </summary>
    /// <returns>A list of all <see cref="Customer"/> records.</returns>
    public async Task<List<Customer>> GetAllCustomersAsync()
    {
        return await _context.Customers.ToListAsync();
    }

    /// <summary>
    /// Retrieves a single customer by ID, eagerly loading their orders.
    /// </summary>
    /// <param name="id">The customer's unique identifier.</param>
    /// <returns>The matching <see cref="Customer"/> with orders included, or <c>null</c> if not found.</returns>
    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        return await _context.Customers.Include(c => c.Orders).FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <summary>
    /// Creates a new customer and persists it to the database.
    /// </summary>
    /// <param name="customer">The customer entity to create.</param>
    /// <returns>The created <see cref="Customer"/> with its generated ID.</returns>
    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }
}
