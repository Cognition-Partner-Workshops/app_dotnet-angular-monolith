using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Models;

namespace OrderManager.Api.Data;

/// <summary>
/// Entity Framework Core database context for the OrderManager monolith.
/// </summary>
/// <remarks>
/// This single context owns all four domain tables (Customers, Products, Orders, Inventory),
/// representing the tight coupling that is characteristic of the monolithic architecture.
/// See <c>docs/decomposition-plan.md</c> in the IaC repo for the planned microservices split.
/// </remarks>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes the context with the supplied options (e.g., SQLite connection string).
    /// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>Gets the set of all <see cref="Customer"/> records.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Gets the set of all <see cref="Product"/> records.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>Gets the set of all <see cref="Order"/> records.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Gets the set of all <see cref="OrderItem"/> records.</summary>
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    /// <summary>Gets the set of all <see cref="InventoryItem"/> records.</summary>
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    /// <summary>
    /// Configures entity relationships, constraints, and column types for the shared database schema.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Customer: unique email, required name (max 200 chars)
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Product: unique SKU, decimal(18,2) price
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Sku).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Sku).IsUnique();
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
        });

        // Order: belongs to Customer, decimal(18,2) total
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Customer).WithMany(c => c.Orders).HasForeignKey(e => e.CustomerId);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
        });

        // OrderItem: joins Order and Product, decimal(18,2) unit price
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
