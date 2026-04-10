using Microsoft.EntityFrameworkCore;
using RfpCopilot.Api.Models;

namespace RfpCopilot.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<RfpDocument> RfpDocuments => Set<RfpDocument>();
    public DbSet<RfpTrackerEntry> RfpTrackerEntries => Set<RfpTrackerEntry>();
    public DbSet<RfpResponseSection> RfpResponseSections => Set<RfpResponseSection>();
    public DbSet<AgentExecutionLog> AgentExecutionLogs => Set<AgentExecutionLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RfpDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ClientName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.OriginatorEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Priority).HasMaxLength(20);
            entity.Property(e => e.PreferredCloudProvider).HasMaxLength(20);
            entity.Property(e => e.ContentType).HasMaxLength(100);
        });

        modelBuilder.Entity<RfpTrackerEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RfpId).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.RfpId).IsUnique();
            entity.Property(e => e.RfpTitle).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ClientName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.OriginatorEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Priority).HasMaxLength(20);
            entity.HasOne(e => e.RfpDocument)
                .WithOne(d => d.TrackerEntry)
                .HasForeignKey<RfpTrackerEntry>(e => e.RfpDocumentId);
        });

        modelBuilder.Entity<RfpResponseSection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SectionTitle).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.RfpDocument)
                .WithMany(d => d.ResponseSections)
                .HasForeignKey(e => e.RfpDocumentId);
        });

        modelBuilder.Entity<AgentExecutionLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AgentName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.RfpDocument)
                .WithMany(d => d.ExecutionLogs)
                .HasForeignKey(e => e.RfpDocumentId);
        });
    }
}
