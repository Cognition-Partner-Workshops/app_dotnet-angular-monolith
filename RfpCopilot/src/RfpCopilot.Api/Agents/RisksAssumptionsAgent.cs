using Microsoft.SemanticKernel;
using RfpCopilot.Api.Models;

namespace RfpCopilot.Api.Agents;

public class RisksAssumptionsAgent : BaseContentAgent
{
    protected override string AgentName => "RisksAssumptionsAgent";
    protected override int SectionNumber => 7;
    protected override string SectionTitle => "Assumptions & Risks";
    protected override string PromptFileName => "risks-assumptions-prompt.txt";

    public RisksAssumptionsAgent(Kernel kernel, ILogger<RisksAssumptionsAgent> logger) : base(kernel, logger) { }
}
