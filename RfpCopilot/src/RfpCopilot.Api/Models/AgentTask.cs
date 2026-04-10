namespace RfpCopilot.Api.Models;

public class AgentTask
{
    public string AgentName { get; set; } = string.Empty;
    public int SectionNumber { get; set; }
    public string RfpContent { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string? CrmId { get; set; }
    public bool IsCloudMigrationInScope { get; set; }
    public string? PreferredCloudProvider { get; set; }
    public Dictionary<string, string> AdditionalContext { get; set; } = new();
}
