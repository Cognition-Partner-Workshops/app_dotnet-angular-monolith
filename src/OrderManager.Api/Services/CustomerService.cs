using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Provides business logic for managing <see cref="Customer"/> entities,
/// including retrieval and creation operations.
/// </summary>
public class CustomerService
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="CustomerService"/>.
    /// </summary>
    /// <param name="context">The database context used for data access.</param>
    public CustomerService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all customers from the database.
    /// </summary>
    /// <returns>A list of all <see cref="Customer"/> records.</returns>
    public async Task<List<Customer>> GetAllCustomersAsync()
    {
        return await _context.Customers.ToListAsync();
    }

    /// <summary>
    /// Retrieves a single customer by ID, including their associated orders.
    /// </summary>
    /// <param name="id">The unique identifier of the customer.</param>
    /// <returns>The matching <see cref="Customer"/> with eagerly loaded orders, or <c>null</c> if not found.</returns>
    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        return await _context.Customers.Include(c => c.Orders).FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <summary>
    /// Creates a new customer record in the database.
    /// </summary>
    /// <param name="customer">The customer entity to persist.</param>
    /// <returns>The created <see cref="Customer"/> with its generated ID.</returns>
    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }
}
