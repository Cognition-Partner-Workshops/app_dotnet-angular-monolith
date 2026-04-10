using Microsoft.SemanticKernel;
using RfpCopilot.Api.Models;

namespace RfpCopilot.Api.Agents;

public class EstimationAgent : BaseContentAgent
{
    protected override string AgentName => "EstimationAgent";
    protected override int SectionNumber => 2;
    protected override string SectionTitle => "Estimation Approach & Estimates";
    protected override string PromptFileName => "estimation-prompt.txt";

    public EstimationAgent(Kernel kernel, ILogger<EstimationAgent> logger) : base(kernel, logger) { }
}
