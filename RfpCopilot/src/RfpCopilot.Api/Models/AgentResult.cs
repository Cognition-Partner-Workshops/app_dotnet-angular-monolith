namespace RfpCopilot.Api.Models;

public class AgentResult
{
    public string AgentName { get; set; } = string.Empty;
    public int SectionNumber { get; set; }
    public string SectionTitle { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int TokensUsed { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
