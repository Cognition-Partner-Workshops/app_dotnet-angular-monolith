namespace RfpCopilot.Api.Models;

public class RfpTrackerEntry
{
    public int Id { get; set; }
    public string RfpId { get; set; } = string.Empty;
    public int? RfpDocumentId { get; set; }
    public string RfpTitle { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string? CrmId { get; set; }
    public string OriginatorName { get; set; } = string.Empty;
    public string OriginatorEmail { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = "New";
    public string? AssignedTo { get; set; }
    public string Priority { get; set; } = "Medium";
    public string? Notes { get; set; }
    public bool EmailSentForMissingCrm { get; set; }
    public DateTime? EmailSentAt { get; set; }

    public RfpDocument? RfpDocument { get; set; }
}
