using System.ComponentModel.DataAnnotations;

namespace OrderManager.Api.DTOs;

public class InitiateCallRequest
{
    [Required]
    public int ReceiverId { get; set; }

    [Required]
    public string CallType { get; set; } = "Audio";
}

public class CallLogDto
{
    public int Id { get; set; }
    public UserDto Caller { get; set; } = null!;
    public UserDto Receiver { get; set; } = null!;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int DurationSeconds { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CallType { get; set; } = string.Empty;
}

public class ContactDto
{
    public int Id { get; set; }
    public int ContactUserId { get; set; }
    public string? DisplayName { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LastSeen { get; set; }
}

public class AddContactRequest
{
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [StringLength(100)]
    public string? DisplayName { get; set; }
}

public class SignalMessage
{
    public int TargetUserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
}
