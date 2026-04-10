namespace RfpCopilot.Api.Models;

public class RfpResponseSection
{
    public int Id { get; set; }
    public int RfpDocumentId { get; set; }
    public int SectionNumber { get; set; }
    public string SectionTitle { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RegeneratedAt { get; set; }
    public string Status { get; set; } = "Pending";

    public RfpDocument? RfpDocument { get; set; }
}
