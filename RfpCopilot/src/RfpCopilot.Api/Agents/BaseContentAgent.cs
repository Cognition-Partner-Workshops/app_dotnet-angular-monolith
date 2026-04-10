using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using RfpCopilot.Api.Models;

namespace RfpCopilot.Api.Agents;

public abstract class BaseContentAgent
{
    protected readonly Kernel Kernel;
    protected readonly ILogger Logger;
    protected readonly string SystemPrompt;
    protected abstract string AgentName { get; }
    protected abstract int SectionNumber { get; }
    protected abstract string SectionTitle { get; }
    protected abstract string PromptFileName { get; }

    protected BaseContentAgent(Kernel kernel, ILogger logger)
    {
        Kernel = kernel;
        Logger = logger;

        var promptPath = Path.Combine(AppContext.BaseDirectory, "Prompts", PromptFileName);
        SystemPrompt = File.Exists(promptPath) ? File.ReadAllText(promptPath) : $"Generate the {SectionTitle} section for an RFP response.";
    }

    public virtual async Task<AgentResult> ExecuteAsync(AgentTask task)
    {
        Logger.LogInformation("{AgentName} started processing", AgentName);

        try
        {
            var prompt = PreparePrompt(task);

            var chatService = Kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(prompt);
            chatHistory.AddUserMessage($"Generate the {SectionTitle} section based on this RFP content:\n\n{task.RfpContent[..Math.Min(task.RfpContent.Length, 6000)]}");

            var response = await chatService.GetChatMessageContentAsync(chatHistory);
            var content = response.Content ?? $"*{SectionTitle} content generation pending - AI service not configured.*";

            Logger.LogInformation("{AgentName} completed successfully", AgentName);

            return new AgentResult
            {
                AgentName = AgentName,
                SectionNumber = SectionNumber,
                SectionTitle = SectionTitle,
                Content = content,
                Success = true,
                TokensUsed = 0,
                CompletedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "{AgentName} LLM call failed, using template response", AgentName);

            // Return template content when LLM is not available
            return new AgentResult
            {
                AgentName = AgentName,
                SectionNumber = SectionNumber,
                SectionTitle = SectionTitle,
                Content = GetFallbackContent(task),
                Success = true,
                TokensUsed = 0,
                CompletedAt = DateTime.UtcNow
            };
        }
    }

    protected virtual string PreparePrompt(AgentTask task)
    {
        return SystemPrompt;
    }

    protected virtual string GetFallbackContent(AgentTask task)
    {
        return SystemPrompt;
    }
}
