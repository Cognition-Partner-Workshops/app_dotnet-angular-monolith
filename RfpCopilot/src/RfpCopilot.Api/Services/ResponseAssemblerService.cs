using Microsoft.EntityFrameworkCore;
using RfpCopilot.Api.Data;
using RfpCopilot.Api.Models;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace RfpCopilot.Api.Services;

public interface IResponseAssemblerService
{
    Task AssembleResponseAsync(int rfpDocumentId, List<AgentResult> results);
    Task<List<RfpResponseSection>> GetResponseSectionsAsync(int rfpDocumentId);
    Task<RfpResponseSection?> GetResponseSectionAsync(int rfpDocumentId, int sectionNumber);
    Task<byte[]> GenerateDocxAsync(int rfpDocumentId);
    Task<byte[]> GeneratePdfAsync(int rfpDocumentId);
}

public class ResponseAssemblerService : IResponseAssemblerService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ResponseAssemblerService> _logger;

    public ResponseAssemblerService(AppDbContext context, ILogger<ResponseAssemblerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AssembleResponseAsync(int rfpDocumentId, List<AgentResult> results)
    {
        // Remove existing sections for this document
        var existingSections = await _context.RfpResponseSections
            .Where(s => s.RfpDocumentId == rfpDocumentId)
            .ToListAsync();
        _context.RfpResponseSections.RemoveRange(existingSections);

        // Add new sections
        foreach (var result in results.OrderBy(r => r.SectionNumber))
        {
            var section = new RfpResponseSection
            {
                RfpDocumentId = rfpDocumentId,
                SectionNumber = result.SectionNumber,
                SectionTitle = result.SectionTitle,
                Content = result.Content,
                GeneratedAt = result.CompletedAt,
                Status = result.Success ? "Completed" : "Failed"
            };
            _context.RfpResponseSections.Add(section);
        }

        // Update document status
        var document = await _context.RfpDocuments.FindAsync(rfpDocumentId);
        if (document != null)
        {
            document.Status = results.All(r => r.Success) ? "Completed" : "Partially Completed";
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Assembled {Count} response sections for RFP document {Id}", results.Count, rfpDocumentId);
    }

    public async Task<List<RfpResponseSection>> GetResponseSectionsAsync(int rfpDocumentId)
    {
        return await _context.RfpResponseSections
            .Where(s => s.RfpDocumentId == rfpDocumentId)
            .OrderBy(s => s.SectionNumber)
            .ToListAsync();
    }

    public async Task<RfpResponseSection?> GetResponseSectionAsync(int rfpDocumentId, int sectionNumber)
    {
        return await _context.RfpResponseSections
            .FirstOrDefaultAsync(s => s.RfpDocumentId == rfpDocumentId && s.SectionNumber == sectionNumber);
    }

    public async Task<byte[]> GenerateDocxAsync(int rfpDocumentId)
    {
        var sections = await GetResponseSectionsAsync(rfpDocumentId);
        var document = await _context.RfpDocuments.FindAsync(rfpDocumentId);

        using var stream = new MemoryStream();
        using (var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
            var body = mainPart.Document.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Body());

            // Title
            var titlePara = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
            var titleRun = titlePara.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run());
            titleRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.RunProperties
            {
                Bold = new DocumentFormat.OpenXml.Wordprocessing.Bold(),
                FontSize = new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = "48" }
            });
            titleRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text($"RFP Response - {document?.ClientName ?? "Unknown Client"}"));

            // Add each section
            foreach (var section in sections)
            {
                // Section header
                var headerPara = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                var headerRun = headerPara.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run());
                headerRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.RunProperties
                {
                    Bold = new DocumentFormat.OpenXml.Wordprocessing.Bold(),
                    FontSize = new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = "32" }
                });
                headerRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text($"Section {section.SectionNumber}: {section.SectionTitle}"));

                // Section content
                var lines = section.Content.Split('\n');
                foreach (var line in lines)
                {
                    var contentPara = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                    var contentRun = contentPara.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run());
                    contentRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(line));
                }
            }
        }

        return stream.ToArray();
    }

    public async Task<byte[]> GeneratePdfAsync(int rfpDocumentId)
    {
        var sections = await GetResponseSectionsAsync(rfpDocumentId);
        var rfpDoc = await _context.RfpDocuments.FindAsync(rfpDocumentId);

        // Use QuestPDF for PDF generation
        QuestPDF.Settings.License = LicenseType.Community;

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                page.Header().Text($"RFP Response - {rfpDoc?.ClientName ?? "Unknown Client"}")
                    .FontSize(20).Bold().FontColor(Colors.Blue.Darken3);

                page.Content().PaddingVertical(10).Column(column =>
                {
                    foreach (var section in sections)
                    {
                        column.Item().PaddingTop(15).Text($"Section {section.SectionNumber}: {section.SectionTitle}")
                            .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

                        column.Item().PaddingTop(5).Text(section.Content).FontSize(10).LineHeight(1.4f);
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();

        return pdfBytes;
    }
}
