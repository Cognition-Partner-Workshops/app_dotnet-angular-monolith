using Microsoft.SemanticKernel;
using RfpCopilot.Api.Models;

namespace RfpCopilot.Api.Agents;

public class IntegrationAgent : BaseContentAgent
{
    protected override string AgentName => "IntegrationAgent";
    protected override int SectionNumber => 4;
    protected override string SectionTitle => "Integration Strategy";
    protected override string PromptFileName => "integration-prompt.txt";

    public IntegrationAgent(Kernel kernel, ILogger<IntegrationAgent> logger) : base(kernel, logger) { }
}
