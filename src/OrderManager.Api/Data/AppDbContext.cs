using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Models;

namespace OrderManager.Api.Data;

/// <summary>
/// EF Core database context for the OrderManager monolith.
/// Exposes DbSets for all domain entities and configures the relational
/// schema (keys, indexes, relationships) in <see cref="OnModelCreating"/>.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    /// <summary>
    /// Configures entity constraints, indexes, and relationships via the Fluent API.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --- Customer: unique email, required name -------------------------
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // --- Product: unique SKU, decimal precision for price --------------
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Sku).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Sku).IsUnique();
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
        });

        // --- Order: belongs to Customer, decimal precision for total -------
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Customer).WithMany(c => c.Orders).HasForeignKey(e => e.CustomerId);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
        });

        // --- OrderItem: many-to-one with Order and Product -----------------
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Order).WithMany(o => o.Items).HasForeignKey(e => e.OrderId);
            entity.HasOne(e => e.Product).WithMany(p => p.OrderItems).HasForeignKey(e => e.ProductId);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
        });

        // --- InventoryItem: one-to-one with Product -----------------------
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Product).WithOne(p => p.Inventory).HasForeignKey<InventoryItem>(e => e.ProductId);
        });
    }
}
