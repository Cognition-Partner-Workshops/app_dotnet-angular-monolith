using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Models;

namespace OrderManager.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    // TrainConnect models
    public DbSet<User> AppUsers => Set<User>();
    public DbSet<Reel> Reels => Set<Reel>();
    public DbSet<ReelLike> ReelLikes => Set<ReelLike>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<CallLog> CallLogs => Set<CallLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Sku).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Sku).IsUnique();
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Customer).WithMany(c => c.Orders).HasForeignKey(e => e.CustomerId);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Order).WithMany(o => o.Items).HasForeignKey(e => e.OrderId);
            entity.HasOne(e => e.Product).WithMany(p => p.OrderItems).HasForeignKey(e => e.ProductId);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Product).WithOne(p => p.Inventory).HasForeignKey<InventoryItem>(e => e.ProductId);
        });

        // TrainConnect entity configurations
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Reel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.VideoUrl).IsRequired().HasMaxLength(500);
            entity.HasOne(e => e.User).WithMany(u => u.Reels).HasForeignKey(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<ReelLike>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Reel).WithMany(r => r.Likes).HasForeignKey(e => e.ReelId);
            entity.HasOne(e => e.User).WithMany(u => u.ReelLikes).HasForeignKey(e => e.UserId);
            entity.HasIndex(e => new { e.ReelId, e.UserId }).IsUnique();
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User).WithMany(u => u.Contacts).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ContactUser).WithMany().HasForeignKey(e => e.ContactUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.UserId, e.ContactUserId }).IsUnique();
        });

        modelBuilder.Entity<CallLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Caller).WithMany(u => u.OutgoingCalls).HasForeignKey(e => e.CallerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Receiver).WithMany(u => u.IncomingCalls).HasForeignKey(e => e.ReceiverId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.StartedAt);
        });
    }
}
