using Microsoft.SemanticKernel;
using RfpCopilot.Api.Models;

namespace RfpCopilot.Api.Agents;

public class CloudMigrationAgent : BaseContentAgent
{
    protected override string AgentName => "CloudMigrationAgent";
    protected override int SectionNumber => 3;
    protected override string SectionTitle => "Architecture Modernization & Cloud Migration";
    protected override string PromptFileName => "cloud-migration-prompt.txt";

    public CloudMigrationAgent(Kernel kernel, ILogger<CloudMigrationAgent> logger) : base(kernel, logger) { }

    public override async Task<AgentResult> ExecuteAsync(AgentTask task)
    {
        if (!task.IsCloudMigrationInScope)
        {
            Logger.LogInformation("Cloud migration not in scope, skipping CloudMigrationAgent");
            return new AgentResult
            {
                AgentName = AgentName,
                SectionNumber = SectionNumber,
                SectionTitle = SectionTitle,
                Content = "## Architecture Modernization & Cloud Migration\n\n> **Note:** Cloud migration is not in scope per client direction. This section has been intentionally omitted from the response.\n\nIf cloud migration becomes relevant in the future, we can provide a comprehensive migration strategy covering current-state assessment, target architecture design, 6R migration framework application, data migration planning, and cloud cost optimization.",
                Success = true,
                TokensUsed = 0,
                CompletedAt = DateTime.UtcNow
            };
        }

        return await base.ExecuteAsync(task);
    }

    protected override string PreparePrompt(AgentTask task)
    {
        var provider = task.PreferredCloudProvider ?? "Azure";
        return SystemPrompt.Replace("{{CLOUD_PROVIDER}}", provider);
    }
}
