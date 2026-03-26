using System.ComponentModel.DataAnnotations;

namespace OrderManager.Api.Models;

public class Reel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [StringLength(500)]
    public string VideoUrl { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ThumbnailUrl { get; set; }

    public int DurationSeconds { get; set; }

    public long ViewCount { get; set; }

    public long LikeCount { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(500)]
    public string? Tags { get; set; }

    public bool IsDownloadable { get; set; } = true;

    public long FileSizeBytes { get; set; }

    public ICollection<ReelLike> Likes { get; set; } = new List<ReelLike>();
}
