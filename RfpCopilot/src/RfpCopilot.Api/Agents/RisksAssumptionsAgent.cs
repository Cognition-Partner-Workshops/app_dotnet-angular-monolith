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

    protected override string GetFallbackContent(AgentTask task)
    {
        var client = task.ClientName ?? "the Client";
        return $@"## Assumptions & Risks

### Assumptions

#### Technical Assumptions

1. **T-01**: The existing infrastructure supports .NET 8 and Angular 17+ runtime requirements
2. **T-02**: Azure OpenAI (or equivalent LLM service) API access will be provisioned within the first 2 weeks of the project
3. **T-03**: All third-party APIs referenced in the RFP have documented, stable APIs with sandbox/test environments
4. **T-04**: The client's network infrastructure supports WebSocket connections (required for SignalR real-time updates)
5. **T-05**: Browser compatibility requirement is limited to modern browsers (Chrome, Edge, Firefox, Safari — last 2 major versions)

#### Business Assumptions

6. **B-01**: {client} will assign a dedicated Product Owner available for at minimum 4 hours/week for sprint reviews and backlog grooming
7. **B-02**: Business requirements are 80% finalized at project start; remaining 20% will be clarified during Discovery (Phase 1)
8. **B-03**: The RFP response process described in the document reflects the current and desired future-state workflow
9. **B-04**: {client} has existing content/templates that can serve as training data for AI agents
10. **B-05**: No regulatory changes impacting project scope are anticipated during the delivery period

#### Resource Assumptions

11. **R-01**: {client} will provide Subject Matter Experts (SMEs) within 48 business hours of any request
12. **R-02**: Key client stakeholders will be available for scheduled workshops and reviews with at least 1 week advance notice
13. **R-03**: Our proposed team will remain stable throughout the project; any resource changes will have a 2-week transition period

#### Timeline Assumptions

14. **TL-01**: The project start date is within 4 weeks of contract signing
15. **TL-02**: Environment provisioning (dev, QA, staging) will be completed within the first 3 weeks
16. **TL-03**: UAT will begin within 1 week of system testing completion; any delays reduce the UAT window

#### Infrastructure Assumptions

17. **I-01**: Cloud subscription (Azure/AWS/GCP) is provisioned and accessible to the development team before Phase 2
18. **I-02**: VPN/network access to client systems will be established within 2 weeks of project kick-off
19. **I-03**: CI/CD pipeline infrastructure (GitHub Actions or Azure DevOps) is available from Day 1

---

### Risk Register

| Risk ID | Category | Description | Probability | Impact | Risk Score | Mitigation Strategy | Owner |
|---------|----------|-------------|:-----------:|:------:|:----------:|---------------------|-------|
| **R-001** | Technical | LLM API rate limits or outages cause agent processing delays | Medium | High | **High** | Implement retry logic with exponential backoff; cache successful responses; design fallback content generation | Tech Lead |
| **R-002** | Technical | AI-generated content quality does not meet {client}'s standards | Medium | High | **High** | Implement human-in-the-loop review workflow; iteratively tune prompts; establish quality benchmarks early | AI/ML Engineer |
| **R-003** | Technical | Performance degradation under concurrent multi-agent execution | Medium | Medium | **Medium** | Load test early in Sprint 3; implement agent execution queuing; optimize parallel processing | Solution Architect |
| **R-004** | Integration | Third-party API changes or deprecations break integrations | Low | High | **Medium** | Use API versioning; implement contract testing; monitor vendor changelogs; build adapter pattern | Tech Lead |
| **R-005** | Integration | CRM system integration complexity exceeds estimates | Medium | Medium | **Medium** | Conduct integration POC in Phase 2; allocate buffer in integration sprints; engage CRM vendor support | Tech Lead |
| **R-006** | Resource | Key team member attrition during critical delivery phases | Low | High | **Medium** | Cross-train team members; maintain updated documentation; have bench resources identified | Project Manager |
| **R-007** | Resource | Client SME availability delays requirement clarification | Medium | Medium | **Medium** | Schedule SME sessions 2 weeks ahead; document assumptions when SME unavailable; escalate through governance | Business Analyst |
| **R-008** | Schedule | Scope creep from evolving requirements delays delivery | High | High | **Critical** | Strict change control process; prioritize MVP features; maintain product backlog with clear prioritization | Project Manager |
| **R-009** | Schedule | Environment provisioning delays impact development start | Medium | High | **High** | Begin environment setup in Week 1; use local Docker containers as interim; escalate blockers immediately | DevOps Engineer |
| **R-010** | Security | Data privacy concerns with AI processing of sensitive RFP content | Medium | High | **High** | Use enterprise-grade Azure OpenAI (data not used for training); implement data masking; conduct security review | Solution Architect |
| **R-011** | Security | Vulnerability in third-party dependencies (supply chain risk) | Medium | High | **High** | Automated dependency scanning (Dependabot/Snyk); regular patching cycle; SBOM maintenance | DevOps Engineer |
| **R-012** | Compliance | Regulatory requirements change during project lifecycle | Low | High | **Medium** | Monitor regulatory landscape; build configurable compliance rules; maintain compliance documentation | Business Analyst |
| **R-013** | Change Mgmt | End-user resistance to AI-driven RFP response workflow | Medium | Medium | **Medium** | Early stakeholder engagement; user training program; champion network; gradual rollout with feedback loops | Project Manager |
| **R-014** | Vendor | Azure OpenAI pricing changes increase operational costs | Low | Medium | **Low** | Monitor Azure pricing updates; implement token usage optimization; design for LLM provider abstraction | Solution Architect |
| **R-015** | Technical | Document parsing accuracy issues with complex PDF/DOCX formats | Medium | Medium | **Medium** | Test with diverse document samples early; implement manual upload fallback; plan for Azure AI Document Intelligence integration | AI/ML Engineer |
| **R-016** | Technical | Browser compatibility issues with Angular Material components | Low | Low | **Low** | Automated cross-browser testing in CI; use only stable Angular Material components; browser testing in QA | QA Lead |
| **R-017** | Schedule | UAT extends beyond planned duration due to defect volume | Medium | Medium | **Medium** | Rigorous system testing before UAT; daily defect triage; pre-UAT readiness checklist | QA Lead |
| **R-018** | Infrastructure | Cloud infrastructure costs exceed budget projections | Medium | Medium | **Medium** | Implement cost monitoring dashboards; use reserved instances; right-size resources; monthly cost reviews | DevOps Engineer |

### Risk Response Strategies

| Strategy | When to Use | Example |
|----------|------------|---------|
| **Avoid** | Eliminate the risk entirely by changing the approach | Use managed services instead of self-hosted to avoid infrastructure management risks |
| **Mitigate** | Reduce probability or impact through proactive actions | Implement automated testing to reduce defect escape rate |
| **Transfer** | Shift risk ownership to a third party | Use Azure SLA guarantees for infrastructure availability |
| **Accept** | Acknowledge risk and prepare contingency plan | Accept minor UI inconsistencies across browsers with documented workarounds |

### Risk Monitoring & Escalation

- **Risk Review Cadence**: Bi-weekly during sprint retrospectives; weekly during critical phases (UAT, cutover)
- **Risk Dashboard**: Real-time risk tracker accessible to all stakeholders via project portal
- **Escalation Path**: Risk Owner → Project Manager → Steering Committee (within 24 hours for Critical risks)
- **Risk Metrics**: Track risk score trends, mitigation effectiveness, and new risk identification rate";
    }
}
