using Microsoft.SemanticKernel;
using RfpCopilot.Api.Models;

namespace RfpCopilot.Api.Agents;

public class SolutionApproachAgent : BaseContentAgent
{
    protected override string AgentName => "SolutionApproachAgent";
    protected override int SectionNumber => 1;
    protected override string SectionTitle => "AI-First Solution Approach";
    protected override string PromptFileName => "solution-approach-prompt.txt";

    public SolutionApproachAgent(Kernel kernel, ILogger<SolutionApproachAgent> logger) : base(kernel, logger) { }
}
