using Microsoft.AspNetCore.Mvc;
using RfpCopilot.Api.Services;

namespace RfpCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResponseController : ControllerBase
{
    private readonly IResponseAssemblerService _responseService;
    private readonly ILogger<ResponseController> _logger;

    public ResponseController(IResponseAssemblerService responseService, ILogger<ResponseController> logger)
    {
        _responseService = responseService;
        _logger = logger;
    }

    [HttpGet("{rfpId}/download/docx")]
    public async Task<IActionResult> DownloadDocx(int rfpId)
    {
        try
        {
            var bytes = await _responseService.GenerateDocxAsync(rfpId);
            return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"rfp-response-{rfpId}.docx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate DOCX for RFP {Id}", rfpId);
            return StatusCode(500, "Failed to generate DOCX document.");
        }
    }

    [HttpGet("{rfpId}/download/pdf")]
    public async Task<IActionResult> DownloadPdf(int rfpId)
    {
        try
        {
            var bytes = await _responseService.GeneratePdfAsync(rfpId);
            return File(bytes, "application/pdf", $"rfp-response-{rfpId}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF for RFP {Id}", rfpId);
            return StatusCode(500, "Failed to generate PDF document.");
        }
    }
}
