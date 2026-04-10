namespace RfpCopilot.Api.Models;

public class RfpDocument
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string ExtractedText { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string ClientName { get; set; } = string.Empty;
    public string? CrmId { get; set; }
    public string OriginatorEmail { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string Priority { get; set; } = "Medium";
    public bool IsCloudMigrationInScope { get; set; }
    public string? PreferredCloudProvider { get; set; }
    public string Status { get; set; } = "Uploaded";

    public ICollection<RfpResponseSection> ResponseSections { get; set; } = new List<RfpResponseSection>();
    public RfpTrackerEntry? TrackerEntry { get; set; }
    public ICollection<AgentExecutionLog> ExecutionLogs { get; set; } = new List<AgentExecutionLog>();
}
