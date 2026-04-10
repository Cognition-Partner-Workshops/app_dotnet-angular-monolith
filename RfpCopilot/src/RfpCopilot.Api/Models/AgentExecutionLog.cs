namespace RfpCopilot.Api.Models;

public class AgentExecutionLog
{
    public int Id { get; set; }
    public int RfpDocumentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "Pending";
    public int TokensUsed { get; set; }
    public string? ErrorMessage { get; set; }

    public RfpDocument? RfpDocument { get; set; }
}
