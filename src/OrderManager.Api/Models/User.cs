using System.ComponentModel.DataAnnotations;

namespace OrderManager.Api.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    public bool IsOnline { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }

    public ICollection<Reel> Reels { get; set; } = new List<Reel>();
    public ICollection<ReelLike> ReelLikes { get; set; } = new List<ReelLike>();
    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
    public ICollection<CallLog> OutgoingCalls { get; set; } = new List<CallLog>();
    public ICollection<CallLog> IncomingCalls { get; set; } = new List<CallLog>();
}
