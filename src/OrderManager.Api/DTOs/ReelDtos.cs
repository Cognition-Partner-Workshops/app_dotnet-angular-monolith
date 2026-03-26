using System.ComponentModel.DataAnnotations;

namespace OrderManager.Api.DTOs;

public class CreateReelRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? Tags { get; set; }

    public bool IsDownloadable { get; set; } = true;
}

public class ReelDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string VideoUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int DurationSeconds { get; set; }
    public long ViewCount { get; set; }
    public long LikeCount { get; set; }
    public string? Tags { get; set; }
    public bool IsDownloadable { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserDto Creator { get; set; } = null!;
    public bool IsLikedByCurrentUser { get; set; }
}

public class ReelFeedResponse
{
    public List<ReelDto> Reels { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}
