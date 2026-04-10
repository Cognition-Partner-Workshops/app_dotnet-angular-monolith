using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using RfpCopilot.Api.Data;
using RfpCopilot.Api.Models;

namespace RfpCopilot.Api.Services;

public interface ITrackerService
{
    Task<List<RfpTrackerEntry>> GetAllEntriesAsync();
    Task<RfpTrackerEntry?> GetByRfpIdAsync(string rfpId);
    Task<RfpTrackerEntry> AddEntryAsync(RfpTrackerEntry entry);
    Task<RfpTrackerEntry> UpdateEntryAsync(string rfpId, RfpTrackerEntry entry);
    Task<string> GenerateNextRfpIdAsync();
    Task<byte[]> ExportToExcelAsync();
}

public class TrackerService : ITrackerService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TrackerService> _logger;

    public TrackerService(AppDbContext context, ILogger<TrackerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<RfpTrackerEntry>> GetAllEntriesAsync()
    {
        return await _context.RfpTrackerEntries
            .OrderByDescending(e => e.ReceivedDate)
            .ToListAsync();
    }

    public async Task<RfpTrackerEntry?> GetByRfpIdAsync(string rfpId)
    {
        return await _context.RfpTrackerEntries
            .FirstOrDefaultAsync(e => e.RfpId == rfpId);
    }

    public async Task<RfpTrackerEntry> AddEntryAsync(RfpTrackerEntry entry)
    {
        if (string.IsNullOrEmpty(entry.RfpId))
        {
            entry.RfpId = await GenerateNextRfpIdAsync();
        }
        _context.RfpTrackerEntries.Add(entry);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Added tracker entry {RfpId}", entry.RfpId);
        return entry;
    }

    public async Task<RfpTrackerEntry> UpdateEntryAsync(string rfpId, RfpTrackerEntry updatedEntry)
    {
        var existing = await _context.RfpTrackerEntries.FirstOrDefaultAsync(e => e.RfpId == rfpId);
        if (existing == null)
            throw new KeyNotFoundException($"Tracker entry with RFP ID '{rfpId}' not found.");

        existing.RfpTitle = updatedEntry.RfpTitle ?? existing.RfpTitle;
        existing.ClientName = updatedEntry.ClientName ?? existing.ClientName;
        existing.CrmId = updatedEntry.CrmId ?? existing.CrmId;
        existing.OriginatorName = updatedEntry.OriginatorName ?? existing.OriginatorName;
        existing.OriginatorEmail = updatedEntry.OriginatorEmail ?? existing.OriginatorEmail;
        existing.DueDate = updatedEntry.DueDate ?? existing.DueDate;
        existing.Status = updatedEntry.Status ?? existing.Status;
        existing.AssignedTo = updatedEntry.AssignedTo ?? existing.AssignedTo;
        existing.Priority = updatedEntry.Priority ?? existing.Priority;
        existing.Notes = updatedEntry.Notes ?? existing.Notes;

        // If CRM ID was just added, update status
        if (!string.IsNullOrEmpty(updatedEntry.CrmId) && existing.Status == "Pending CRM")
        {
            existing.Status = "New";
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated tracker entry {RfpId}", rfpId);
        return existing;
    }

    public async Task<string> GenerateNextRfpIdAsync()
    {
        var year = DateTime.UtcNow.Year;
        var lastEntry = await _context.RfpTrackerEntries
            .Where(e => e.RfpId.StartsWith($"RFP-{year}-"))
            .OrderByDescending(e => e.RfpId)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastEntry != null)
        {
            var parts = lastEntry.RfpId.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"RFP-{year}-{nextNumber:D3}";
    }

    public async Task<byte[]> ExportToExcelAsync()
    {
        var entries = await GetAllEntriesAsync();
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("RFP Tracker");

        // Headers
        var headers = new[] { "RFP ID", "RFP Title", "Client Name", "CRM ID", "Originator Name",
            "Originator Email", "Received Date", "Due Date", "Status", "Assigned To", "Priority", "Notes" };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        // Data rows
        for (int row = 0; row < entries.Count; row++)
        {
            var entry = entries[row];
            worksheet.Cell(row + 2, 1).Value = entry.RfpId;
            worksheet.Cell(row + 2, 2).Value = entry.RfpTitle;
            worksheet.Cell(row + 2, 3).Value = entry.ClientName;
            worksheet.Cell(row + 2, 4).Value = entry.CrmId ?? "";
            worksheet.Cell(row + 2, 5).Value = entry.OriginatorName;
            worksheet.Cell(row + 2, 6).Value = entry.OriginatorEmail;
            worksheet.Cell(row + 2, 7).Value = entry.ReceivedDate.ToString("yyyy-MM-dd");
            worksheet.Cell(row + 2, 8).Value = entry.DueDate?.ToString("yyyy-MM-dd") ?? "";
            worksheet.Cell(row + 2, 9).Value = entry.Status;
            worksheet.Cell(row + 2, 10).Value = entry.AssignedTo ?? "";
            worksheet.Cell(row + 2, 11).Value = entry.Priority;
            worksheet.Cell(row + 2, 12).Value = entry.Notes ?? "";

            // Color-code rows
            if (string.IsNullOrEmpty(entry.CrmId))
            {
                for (int col = 1; col <= headers.Length; col++)
                    worksheet.Cell(row + 2, col).Style.Fill.BackgroundColor = XLColor.LightPink;
            }
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
