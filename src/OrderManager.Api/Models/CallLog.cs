namespace OrderManager.Api.Models;

public enum CallStatus
{
    Missed,
    Answered,
    Declined,
    Queued
}

public enum CallType
{
    Audio,
    Video
}

public class CallLog
{
    public int Id { get; set; }

    public int CallerId { get; set; }
    public User Caller { get; set; } = null!;

    public int ReceiverId { get; set; }
    public User Receiver { get; set; } = null!;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAt { get; set; }

    public int DurationSeconds { get; set; }

    public CallStatus Status { get; set; } = CallStatus.Missed;

    public CallType CallType { get; set; } = CallType.Audio;
}
