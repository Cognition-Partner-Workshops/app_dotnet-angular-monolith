using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RfpCopilot.Api.Data;
using RfpCopilot.Api.Models;
using RfpCopilot.Api.Services;
using Xunit;

namespace RfpCopilot.Api.Tests;

public class TrackerServiceTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GenerateNextRfpId_ShouldReturnFirstId_WhenNoEntriesExist()
    {
        using var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<TrackerService>>();
        var service = new TrackerService(context, logger.Object);

        var id = await service.GenerateNextRfpIdAsync();

        Assert.StartsWith("RFP-", id);
        Assert.EndsWith("-001", id);
    }

    [Fact]
    public async Task AddEntry_ShouldPersistEntry()
    {
        using var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<TrackerService>>();
        var service = new TrackerService(context, logger.Object);

        var entry = new RfpTrackerEntry
        {
            RfpTitle = "Test RFP",
            ClientName = "Test Client",
            OriginatorEmail = "test@test.com",
            Status = "New",
            Priority = "High"
        };

        var result = await service.AddEntryAsync(entry);

        Assert.NotNull(result.RfpId);
        Assert.Equal("Test RFP", result.RfpTitle);

        var allEntries = await service.GetAllEntriesAsync();
        Assert.Single(allEntries);
    }

    [Fact]
    public async Task UpdateEntry_ShouldUpdateCrmIdAndStatus()
    {
        using var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<TrackerService>>();
        var service = new TrackerService(context, logger.Object);

        var entry = new RfpTrackerEntry
        {
            RfpId = "RFP-2025-100",
            RfpTitle = "Test RFP",
            ClientName = "Test Client",
            OriginatorEmail = "test@test.com",
            Status = "Pending CRM",
            Priority = "Medium"
        };
        context.RfpTrackerEntries.Add(entry);
        await context.SaveChangesAsync();

        var updated = await service.UpdateEntryAsync("RFP-2025-100", new RfpTrackerEntry
        {
            CrmId = "CRM-2025-100",
            RfpTitle = "Test RFP",
            ClientName = "Test Client",
            OriginatorEmail = "test@test.com",
            Status = "Pending CRM",
            Priority = "Medium"
        });

        Assert.Equal("CRM-2025-100", updated.CrmId);
        Assert.Equal("New", updated.Status);
    }

    [Fact]
    public async Task ExportToExcel_ShouldReturnValidBytes()
    {
        using var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<TrackerService>>();
        var service = new TrackerService(context, logger.Object);

        context.RfpTrackerEntries.Add(new RfpTrackerEntry
        {
            RfpId = "RFP-2025-001",
            RfpTitle = "Test",
            ClientName = "Client",
            OriginatorEmail = "test@test.com",
            Status = "New",
            Priority = "High"
        });
        await context.SaveChangesAsync();

        var bytes = await service.ExportToExcelAsync();

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }
}
