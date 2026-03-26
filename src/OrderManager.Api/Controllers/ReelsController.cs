using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.DTOs;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReelsController : ControllerBase
{
    private readonly ReelService _reelService;

    public ReelsController(ReelService reelService)
    {
        _reelService = reelService;
    }

    [HttpGet]
    public async Task<ActionResult<ReelFeedResponse>> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 10;

        var userId = GetCurrentUserId();
        var feed = await _reelService.GetFeedAsync(page, pageSize, userId);
        return Ok(feed);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ReelDto>> GetById(int id)
    {
        var userId = GetCurrentUserId();
        var reel = await _reelService.GetByIdAsync(id, userId);
        if (reel == null) return NotFound();
        return Ok(reel);
    }

    [Authorize]
    [HttpPost]
    [RequestSizeLimit(104857600)] // 100MB max
    public async Task<ActionResult<ReelDto>> Create([FromForm] CreateReelRequest request, IFormFile? videoFile)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        string videoUrl;
        long fileSizeBytes = 0;
        int durationSeconds = 30;

        if (videoFile != null)
        {
            var allowedTypes = new[] { "video/mp4", "video/webm", "video/quicktime" };
            if (!allowedTypes.Contains(videoFile.ContentType.ToLowerInvariant()))
                return BadRequest(new { error = "Only MP4, WebM, and MOV video files are allowed" });

            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reels");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(videoFile.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await videoFile.CopyToAsync(stream);
            }

            videoUrl = $"/uploads/reels/{fileName}";
            fileSizeBytes = videoFile.Length;
        }
        else
        {
            videoUrl = "/assets/reels/default.mp4";
        }

        var reel = await _reelService.CreateAsync(request, userId.Value, videoUrl, null, durationSeconds, fileSizeBytes);
        return CreatedAtAction(nameof(GetById), new { id = reel.Id }, reel);
    }

    [Authorize]
    [HttpPost("{id}/like")]
    public async Task<ActionResult> ToggleLike(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var isLiked = await _reelService.ToggleLikeAsync(id, userId.Value);
            return Ok(new { isLiked });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("{id}/download")]
    public async Task<ActionResult> Download(int id)
    {
        var reel = await _reelService.GetReelEntityAsync(id);
        if (reel == null) return NotFound();
        if (!reel.IsDownloadable) return Forbid();

        return Ok(new
        {
            downloadUrl = reel.VideoUrl,
            title = reel.Title,
            fileSizeBytes = reel.FileSizeBytes,
            durationSeconds = reel.DurationSeconds
        });
    }

    [Authorize]
    [HttpGet("my")]
    public async Task<ActionResult<List<ReelDto>>> GetMyReels()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var reels = await _reelService.GetUserReelsAsync(userId.Value);
        return Ok(reels);
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out var userId))
            return userId;
        return null;
    }
}
