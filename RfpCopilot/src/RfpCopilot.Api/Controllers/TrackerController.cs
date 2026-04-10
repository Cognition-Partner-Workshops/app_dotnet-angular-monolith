using Microsoft.AspNetCore.Mvc;
using RfpCopilot.Api.Models;
using RfpCopilot.Api.Services;

namespace RfpCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrackerController : ControllerBase
{
    private readonly ITrackerService _trackerService;
    private readonly ILogger<TrackerController> _logger;

    public TrackerController(ITrackerService trackerService, ILogger<TrackerController> logger)
    {
        _trackerService = trackerService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<RfpTrackerEntry>>> GetAll()
    {
        var entries = await _trackerService.GetAllEntriesAsync();
        return Ok(entries);
    }

    [HttpGet("{rfpId}")]
    public async Task<ActionResult<RfpTrackerEntry>> GetByRfpId(string rfpId)
    {
        var entry = await _trackerService.GetByRfpIdAsync(rfpId);
        if (entry == null) return NotFound();
        return Ok(entry);
    }

    [HttpPut("{rfpId}")]
    public async Task<ActionResult<RfpTrackerEntry>> Update(string rfpId, [FromBody] RfpTrackerEntry entry)
    {
        try
        {
            var updated = await _trackerService.UpdateEntryAsync(rfpId, entry);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("export")]
    public async Task<IActionResult> ExportToExcel()
    {
        var bytes = await _trackerService.ExportToExcelAsync();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "rfp-tracker.xlsx");
    }
}
