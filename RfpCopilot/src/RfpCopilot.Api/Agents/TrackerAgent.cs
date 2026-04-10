using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using RfpCopilot.Api.Models;
using RfpCopilot.Api.Services;

namespace RfpCopilot.Api.Agents;

public class TrackerAgent
{
    private readonly Kernel _kernel;
    private readonly ITrackerService _trackerService;
    private readonly IEmailService _emailService;
    private readonly ILogger<TrackerAgent> _logger;
    private readonly string _systemPrompt;

    public TrackerAgent(Kernel kernel, ITrackerService trackerService, IEmailService emailService, ILogger<TrackerAgent> logger)
    {
        _kernel = kernel;
        _trackerService = trackerService;
        _emailService = emailService;
        _logger = logger;

        var promptPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "tracker-agent-prompt.txt");
        _systemPrompt = File.Exists(promptPath) ? File.ReadAllText(promptPath) : "Extract RFP metadata from the document.";
    }

    public async Task<AgentResult> ExecuteAsync(AgentTask task)
    {
        _logger.LogInformation("TrackerAgent started for client: {ClientName}", task.ClientName);

        try
        {
            // Extract metadata using LLM
            string rfpTitle = task.ClientName + " RFP";
            string extractedClient = task.ClientName;

            try
            {
                var chatService = _kernel.GetRequiredService<IChatCompletionService>();
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage(_systemPrompt);
                chatHistory.AddUserMessage($"Extract metadata from this RFP document:\n\n{task.RfpContent[..Math.Min(task.RfpContent.Length, 4000)]}");
                var responses = await chatService.GetChatMessageContentsAsync(chatHistory);
                var responseText = responses.FirstOrDefault()?.Content ?? "";

                // Parse the response
                foreach (var line in responseText.Split('\n'))
                {
                    if (line.StartsWith("TITLE:"))
                        rfpTitle = line["TITLE:".Length..].Trim();
                    else if (line.StartsWith("CLIENT:"))
                        extractedClient = line["CLIENT:".Length..].Trim();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM extraction failed, using provided metadata");
            }

            // Create tracker entry
            var newRfpId = await _trackerService.GenerateNextRfpIdAsync();
            var trackerEntry = new RfpTrackerEntry
            {
                RfpId = newRfpId,
                RfpTitle = rfpTitle,
                ClientName = string.IsNullOrEmpty(extractedClient) ? task.ClientName : extractedClient,
                CrmId = task.CrmId,
                OriginatorName = "System",
                OriginatorEmail = task.AdditionalContext.GetValueOrDefault("OriginatorEmail", "abc_xyz@gmail.com"),
                ReceivedDate = DateTime.UtcNow,
                DueDate = task.AdditionalContext.TryGetValue("DueDate", out var dueDateStr) && DateTime.TryParse(dueDateStr, out var dueDate) ? dueDate : null,
                Priority = task.AdditionalContext.GetValueOrDefault("Priority", "Medium"),
                Notes = $"Auto-generated from uploaded RFP document"
            };

            // Check CRM ID
            bool emailSent = false;
            if (string.IsNullOrEmpty(task.CrmId))
            {
                trackerEntry.Status = "Pending CRM";
                // Send email notification
                var emailSubject = $"Action Required: CRM ID Missing for {rfpTitle}";
                var emailBody = $@"
<html>
<body>
<h2>CRM ID Required for RFP Submission</h2>
<p>Dear Originator,</p>
<p>A new RFP has been received and registered in our system, but the <strong>CRM ID is missing</strong>. Please update the tracker with the CRM ID to proceed with the response generation.</p>
<table style='border-collapse: collapse; width: 100%;'>
<tr><td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>RFP ID</td><td style='border: 1px solid #ddd; padding: 8px;'>{newRfpId}</td></tr>
<tr><td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>RFP Title</td><td style='border: 1px solid #ddd; padding: 8px;'>{rfpTitle}</td></tr>
<tr><td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Client Name</td><td style='border: 1px solid #ddd; padding: 8px;'>{trackerEntry.ClientName}</td></tr>
</table>
<p>Please log into the RFP Copilot tracker and update the CRM ID for this entry, or reply to this email with the CRM ID.</p>
<p>Thank you,<br/>RFP Copilot System</p>
</body>
</html>";

                emailSent = await _emailService.SendEmailAsync(trackerEntry.OriginatorEmail, emailSubject, emailBody);
                trackerEntry.EmailSentForMissingCrm = emailSent;
                trackerEntry.EmailSentAt = emailSent ? DateTime.UtcNow : null;
            }
            else
            {
                trackerEntry.Status = "New";
            }

            await _trackerService.AddEntryAsync(trackerEntry);

            var statusNote = string.IsNullOrEmpty(task.CrmId)
                ? $"Created tracker entry {newRfpId} with status 'Pending CRM'. Email notification {(emailSent ? "sent" : "failed")} to {trackerEntry.OriginatorEmail}."
                : $"Created tracker entry {newRfpId} with status 'New'. CRM ID: {task.CrmId}.";

            return new AgentResult
            {
                AgentName = "TrackerAgent",
                SectionNumber = 0,
                SectionTitle = "RFP Tracker Entry",
                Content = $"## RFP Tracker Entry\n\n{statusNote}\n\n| Field | Value |\n|-------|-------|\n| RFP ID | {newRfpId} |\n| Title | {rfpTitle} |\n| Client | {trackerEntry.ClientName} |\n| CRM ID | {task.CrmId ?? "Missing"} |\n| Status | {trackerEntry.Status} |\n| Priority | {trackerEntry.Priority} |",
                Success = true,
                TokensUsed = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TrackerAgent failed");
            return new AgentResult
            {
                AgentName = "TrackerAgent",
                SectionNumber = 0,
                SectionTitle = "RFP Tracker Entry",
                Content = $"Error creating tracker entry: {ex.Message}",
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
