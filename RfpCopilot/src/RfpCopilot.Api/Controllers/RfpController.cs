using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RfpCopilot.Api.Agents;
using RfpCopilot.Api.Data;
using RfpCopilot.Api.Models;
using RfpCopilot.Api.Services;

namespace RfpCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RfpController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IDocumentParserService _documentParser;
    private readonly OrchestratorAgent _orchestrator;
    private readonly ILogger<RfpController> _logger;

    public RfpController(
        AppDbContext context,
        IDocumentParserService documentParser,
        OrchestratorAgent orchestrator,
        ILogger<RfpController> logger)
    {
        _context = context;
        _documentParser = documentParser;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<RfpDocument>> Upload(
        IFormFile file,
        [FromForm] string clientName,
        [FromForm] string? crmId,
        [FromForm] string originatorEmail,
        [FromForm] DateTime? dueDate,
        [FromForm] string priority = "Medium",
        [FromForm] bool isCloudMigrationInScope = false,
        [FromForm] string? preferredCloudProvider = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        _logger.LogInformation("Uploading RFP document: {FileName}", file.FileName);

        // Extract text from the document
        string extractedText;
        try
        {
            using var stream = file.OpenReadStream();
            extractedText = await _documentParser.ExtractTextAsync(stream, file.ContentType, file.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse document");
            return BadRequest($"Failed to parse document: {ex.Message}");
        }

        var rfpDocument = new RfpDocument
        {
            FileName = file.FileName,
            FileSize = file.Length,
            ContentType = file.ContentType,
            ExtractedText = extractedText,
            ClientName = clientName,
            CrmId = string.IsNullOrWhiteSpace(crmId) ? null : crmId,
            OriginatorEmail = originatorEmail,
            DueDate = dueDate,
            Priority = priority,
            IsCloudMigrationInScope = isCloudMigrationInScope,
            PreferredCloudProvider = preferredCloudProvider,
            Status = "Uploaded"
        };

        _context.RfpDocuments.Add(rfpDocument);
        await _context.SaveChangesAsync();

        // Process in background
        _ = Task.Run(async () =>
        {
            try
            {
                await _orchestrator.ProcessRfpAsync(rfpDocument.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background processing failed for RFP {Id}", rfpDocument.Id);
            }
        });

        return CreatedAtAction(nameof(GetStatus), new { id = rfpDocument.Id }, rfpDocument);
    }

    [HttpGet("{id}/status")]
    public async Task<ActionResult> GetStatus(int id)
    {
        var document = await _context.RfpDocuments.FindAsync(id);
        if (document == null) return NotFound();

        var logs = await _context.AgentExecutionLogs
            .Where(l => l.RfpDocumentId == id)
            .OrderBy(l => l.StartedAt)
            .Select(l => new { l.AgentName, l.Status, l.StartedAt, l.CompletedAt, l.ErrorMessage })
            .ToListAsync();

        return Ok(new
        {
            document.Id,
            document.FileName,
            document.ClientName,
            document.Status,
            document.UploadedAt,
            AgentProgress = logs
        });
    }

    [HttpGet("{id}/response")]
    public async Task<ActionResult> GetResponse(int id)
    {
        var document = await _context.RfpDocuments
            .Include(d => d.ResponseSections)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null) return NotFound();

        return Ok(new
        {
            document.Id,
            document.FileName,
            document.ClientName,
            document.Status,
            Sections = document.ResponseSections.OrderBy(s => s.SectionNumber).Select(s => new
            {
                s.SectionNumber,
                s.SectionTitle,
                s.Content,
                s.Status,
                s.GeneratedAt,
                s.RegeneratedAt
            })
        });
    }

    [HttpGet("{id}/response/{section}")]
    public async Task<ActionResult> GetResponseSection(int id, int section)
    {
        var responseSection = await _context.RfpResponseSections
            .FirstOrDefaultAsync(s => s.RfpDocumentId == id && s.SectionNumber == section);

        if (responseSection == null) return NotFound();

        return Ok(responseSection);
    }

    [HttpPost("{id}/regenerate/{section}")]
    public async Task<ActionResult> RegenerateSection(int id, int section)
    {
        var document = await _context.RfpDocuments.FindAsync(id);
        if (document == null) return NotFound();

        _ = Task.Run(async () =>
        {
            try
            {
                await _orchestrator.RegenerateSectionAsync(id, section);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Regeneration failed for RFP {Id} section {Section}", id, section);
            }
        });

        return Accepted(new { Message = $"Regeneration of section {section} started" });
    }

    [HttpGet]
    public async Task<ActionResult<List<RfpDocument>>> GetAll()
    {
        var documents = await _context.RfpDocuments
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new
            {
                d.Id,
                d.FileName,
                d.ClientName,
                d.CrmId,
                d.Status,
                d.Priority,
                d.UploadedAt,
                d.DueDate,
                SectionCount = d.ResponseSections.Count
            })
            .ToListAsync();

        return Ok(documents);
    }
}
