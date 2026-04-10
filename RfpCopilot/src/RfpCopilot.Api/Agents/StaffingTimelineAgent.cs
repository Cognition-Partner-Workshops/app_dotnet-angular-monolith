using Microsoft.SemanticKernel;
using RfpCopilot.Api.Models;

namespace RfpCopilot.Api.Agents;

public class StaffingTimelineAgent : BaseContentAgent
{
    protected override string AgentName => "StaffingTimelineAgent";
    protected override int SectionNumber => 6;
    protected override string SectionTitle => "Staffing & Timelines";
    protected override string PromptFileName => "staffing-timeline-prompt.txt";

    public StaffingTimelineAgent(Kernel kernel, ILogger<StaffingTimelineAgent> logger) : base(kernel, logger) { }
}
