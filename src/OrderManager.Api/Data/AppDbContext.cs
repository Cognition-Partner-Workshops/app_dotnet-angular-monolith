using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Models;

namespace OrderManager.Api.Data;

/// <summary>
/// Entity Framework Core database context for the OrderManager application.
/// Provides access to all domain entity sets and configures the relational schema
/// (keys, indexes, constraints, and relationships) via a single shared SQLite database.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="AppDbContext"/> with the specified options.
    /// </summary>
    /// <param name="options">Database context configuration options (e.g., connection string).</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>Gets the set of all <see cref="Customer"/> entities.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Gets the set of all <see cref="Product"/> entities.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>Gets the set of all <see cref="Order"/> entities.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Gets the set of all <see cref="OrderItem"/> entities.</summary>
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    /// <summary>Gets the set of all <see cref="InventoryItem"/> entities.</summary>
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    /// <summary>
    /// Configures the EF Core model: primary keys, unique indexes, required fields,
    /// column types, and navigation relationships for all domain entities.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Customer: Name and Email required; Email must be unique
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Product: Name and SKU required; SKU must be unique; Price stored as decimal(18,2)
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Sku).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Sku).IsUnique();
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
        });

        // Order: belongs to Customer (many-to-one); TotalAmount stored as decimal(18,2)
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Customer).WithMany(c => c.Orders).HasForeignKey(e => e.CustomerId);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
        });

        // OrderItem: belongs to Order and Product (many-to-one each); UnitPrice as decimal(18,2)
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Order).WithMany(o => o.Items).HasForeignKey(e => e.OrderId);
            entity.HasOne(e => e.Product).WithMany(p => p.OrderItems).HasForeignKey(e => e.ProductId);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
        });

        // InventoryItem: one-to-one with Product
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Product).WithOne(p => p.Inventory).HasForeignKey<InventoryItem>(e => e.ProductId);
        });
    }
}
