using Microsoft.SemanticKernel;
using RfpCopilot.Api.Models;

namespace RfpCopilot.Api.Agents;

public class TestingDevOpsAgent : BaseContentAgent
{
    protected override string AgentName => "TestingDevOpsAgent";
    protected override int SectionNumber => 5;
    protected override string SectionTitle => "Testing, DevOps & Cutover Strategy";
    protected override string PromptFileName => "testing-devops-prompt.txt";

    public TestingDevOpsAgent(Kernel kernel, ILogger<TestingDevOpsAgent> logger) : base(kernel, logger) { }
}
