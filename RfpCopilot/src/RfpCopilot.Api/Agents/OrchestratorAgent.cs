using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using RfpCopilot.Api.Data;
using RfpCopilot.Api.Hubs;
using RfpCopilot.Api.Models;
using RfpCopilot.Api.Services;

namespace RfpCopilot.Api.Agents;

public class OrchestratorAgent
{
    private readonly Kernel _kernel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrchestratorAgent> _logger;

    public OrchestratorAgent(Kernel kernel, IServiceProvider serviceProvider, ILogger<OrchestratorAgent> logger)
    {
        _kernel = kernel;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ProcessRfpAsync(int rfpDocumentId)
    {
        _logger.LogInformation("OrchestratorAgent starting processing for RFP document {Id}", rfpDocumentId);

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var responseAssembler = scope.ServiceProvider.GetRequiredService<IResponseAssemblerService>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<RfpProgressHub>>();

        var document = await context.RfpDocuments.FindAsync(rfpDocumentId);
        if (document == null)
        {
            _logger.LogError("RFP document {Id} not found", rfpDocumentId);
            return;
        }

        document.Status = "Processing";
        await context.SaveChangesAsync();
        await hubContext.Clients.All.SendAsync("ProgressUpdate", rfpDocumentId, "Processing", "Orchestrator started");

        var agentTask = new AgentTask
        {
            RfpContent = document.ExtractedText,
            ClientName = document.ClientName,
            CrmId = document.CrmId,
            IsCloudMigrationInScope = document.IsCloudMigrationInScope,
            PreferredCloudProvider = document.PreferredCloudProvider,
            AdditionalContext = new Dictionary<string, string>
            {
                ["OriginatorEmail"] = document.OriginatorEmail,
                ["Priority"] = document.Priority,
                ["DueDate"] = document.DueDate?.ToString("yyyy-MM-dd") ?? ""
            }
        };

        var results = new List<AgentResult>();

        // Step 0: Tracker Agent (sequential)
        await LogAgentStart(context, hubContext, rfpDocumentId, "TrackerAgent");
        try
        {
            var trackerAgent = scope.ServiceProvider.GetRequiredService<TrackerAgent>();
            var trackerResult = await trackerAgent.ExecuteAsync(agentTask);
            results.Add(trackerResult);
            await LogAgentComplete(context, hubContext, rfpDocumentId, "TrackerAgent", trackerResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TrackerAgent failed");
            await LogAgentFailed(context, hubContext, rfpDocumentId, "TrackerAgent", ex.Message);
        }

        // Steps 1-7: Content agents (parallel)
        var contentTasks = new List<Task<AgentResult>>();

        var agents = new (string Name, Func<IServiceScope, BaseContentAgent> Factory)[]
        {
            ("SolutionApproachAgent", s => s.ServiceProvider.GetRequiredService<SolutionApproachAgent>()),
            ("EstimationAgent", s => s.ServiceProvider.GetRequiredService<EstimationAgent>()),
            ("CloudMigrationAgent", s => s.ServiceProvider.GetRequiredService<CloudMigrationAgent>()),
            ("IntegrationAgent", s => s.ServiceProvider.GetRequiredService<IntegrationAgent>()),
            ("TestingDevOpsAgent", s => s.ServiceProvider.GetRequiredService<TestingDevOpsAgent>()),
            ("StaffingTimelineAgent", s => s.ServiceProvider.GetRequiredService<StaffingTimelineAgent>()),
            ("RisksAssumptionsAgent", s => s.ServiceProvider.GetRequiredService<RisksAssumptionsAgent>())
        };

        foreach (var (name, factory) in agents)
        {
            await LogAgentStart(context, hubContext, rfpDocumentId, name);
            contentTasks.Add(Task.Run(async () =>
            {
                try
                {
                    using var agentScope = _serviceProvider.CreateScope();
                    var agent = factory(agentScope);
                    var result = await agent.ExecuteAsync(agentTask);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{AgentName} failed", name);
                    return new AgentResult
                    {
                        AgentName = name,
                        SectionTitle = name.Replace("Agent", ""),
                        Content = $"Error generating section: {ex.Message}",
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                }
            }));
        }

        var contentResults = await Task.WhenAll(contentTasks);
        foreach (var result in contentResults)
        {
            results.Add(result);
            await LogAgentComplete(context, hubContext, rfpDocumentId, result.AgentName, result.Success);
        }

        // Assemble the response
        await responseAssembler.AssembleResponseAsync(rfpDocumentId, results);

        // Mark as complete
        var docToUpdate = await context.RfpDocuments.FindAsync(rfpDocumentId);
        if (docToUpdate != null)
        {
            var hasMissingCrm = string.IsNullOrEmpty(document.CrmId);
            docToUpdate.Status = hasMissingCrm ? "Draft - Pending CRM ID" : "Completed";
            await context.SaveChangesAsync();
        }

        await hubContext.Clients.All.SendAsync("ProgressUpdate", rfpDocumentId, "Completed", "All agents finished");
        _logger.LogInformation("OrchestratorAgent completed for RFP document {Id}", rfpDocumentId);
    }

    public async Task RegenerateSectionAsync(int rfpDocumentId, int sectionNumber)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var responseAssembler = scope.ServiceProvider.GetRequiredService<IResponseAssemblerService>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<RfpProgressHub>>();

        var document = await context.RfpDocuments.FindAsync(rfpDocumentId);
        if (document == null) return;

        var agentTask = new AgentTask
        {
            RfpContent = document.ExtractedText,
            ClientName = document.ClientName,
            CrmId = document.CrmId,
            IsCloudMigrationInScope = document.IsCloudMigrationInScope,
            PreferredCloudProvider = document.PreferredCloudProvider
        };

        BaseContentAgent? agent = sectionNumber switch
        {
            1 => scope.ServiceProvider.GetRequiredService<SolutionApproachAgent>(),
            2 => scope.ServiceProvider.GetRequiredService<EstimationAgent>(),
            3 => scope.ServiceProvider.GetRequiredService<CloudMigrationAgent>(),
            4 => scope.ServiceProvider.GetRequiredService<IntegrationAgent>(),
            5 => scope.ServiceProvider.GetRequiredService<TestingDevOpsAgent>(),
            6 => scope.ServiceProvider.GetRequiredService<StaffingTimelineAgent>(),
            7 => scope.ServiceProvider.GetRequiredService<RisksAssumptionsAgent>(),
            _ => null
        };

        if (agent == null) return;

        var result = await agent.ExecuteAsync(agentTask);

        var existingSection = await context.RfpResponseSections
            .FirstOrDefaultAsync(s => s.RfpDocumentId == rfpDocumentId && s.SectionNumber == sectionNumber);

        if (existingSection != null)
        {
            existingSection.Content = result.Content;
            existingSection.RegeneratedAt = DateTime.UtcNow;
            existingSection.Status = result.Success ? "Completed" : "Failed";
            await context.SaveChangesAsync();
        }

        await hubContext.Clients.All.SendAsync("SectionRegenerated", rfpDocumentId, sectionNumber);
    }

    private static async Task LogAgentStart(AppDbContext context, IHubContext<RfpProgressHub> hubContext, int rfpDocumentId, string agentName)
    {
        context.AgentExecutionLogs.Add(new AgentExecutionLog
        {
            RfpDocumentId = rfpDocumentId,
            AgentName = agentName,
            StartedAt = DateTime.UtcNow,
            Status = "InProgress"
        });
        await context.SaveChangesAsync();
        await hubContext.Clients.All.SendAsync("AgentProgress", rfpDocumentId, agentName, "InProgress");
    }

    private static async Task LogAgentComplete(AppDbContext context, IHubContext<RfpProgressHub> hubContext, int rfpDocumentId, string agentName, bool success)
    {
        var log = await context.AgentExecutionLogs
            .Where(l => l.RfpDocumentId == rfpDocumentId && l.AgentName == agentName)
            .OrderByDescending(l => l.StartedAt)
            .FirstOrDefaultAsync();
        if (log != null)
        {
            log.CompletedAt = DateTime.UtcNow;
            log.Status = success ? "Completed" : "Failed";
            await context.SaveChangesAsync();
        }
        await hubContext.Clients.All.SendAsync("AgentProgress", rfpDocumentId, agentName, success ? "Completed" : "Failed");
    }

    private static async Task LogAgentFailed(AppDbContext context, IHubContext<RfpProgressHub> hubContext, int rfpDocumentId, string agentName, string error)
    {
        var log = await context.AgentExecutionLogs
            .Where(l => l.RfpDocumentId == rfpDocumentId && l.AgentName == agentName)
            .OrderByDescending(l => l.StartedAt)
            .FirstOrDefaultAsync();
        if (log != null)
        {
            log.CompletedAt = DateTime.UtcNow;
            log.Status = "Failed";
            log.ErrorMessage = error;
            await context.SaveChangesAsync();
        }
        await hubContext.Clients.All.SendAsync("AgentProgress", rfpDocumentId, agentName, "Failed");
    }
}
