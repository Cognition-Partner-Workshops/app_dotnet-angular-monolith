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

    protected override string GetFallbackContent(AgentTask task)
    {
        var client = task.ClientName ?? "the Client";
        return $@"## Estimation Approach & Estimates

### Estimation Methodology

We employ a hybrid estimation approach combining **Function Point Analysis (FPA)**, **Story Point estimation**, and **T-Shirt sizing** to ensure accuracy and transparency. Our methodology has been refined over 500+ enterprise engagements.

**Estimation Process:**
1. **Decomposition**: Break down the RFP scope into functional modules and work packages
2. **Complexity Assessment**: Rate each module as Low/Medium/High complexity
3. **Historical Benchmarking**: Compare against our repository of similar past projects
4. **Expert Judgment**: Senior architects and delivery leads validate estimates
5. **Risk Adjustment**: Apply contingency buffers based on risk profile

### Effort Breakdown by Module

| Module | Complexity | Effort (Person-Days) | Team Size | Duration (Weeks) |
|--------|-----------|---------------------|-----------|------------------|
| **Core Platform & Architecture** | High | 120 | 4 | 6 |
| **AI/ML Engine & Agent Framework** | High | 160 | 5 | 8 |
| **User Interface (Angular SPA)** | Medium | 80 | 3 | 5 |
| **API Development & Integration Layer** | Medium | 100 | 4 | 5 |
| **Database Design & Data Migration** | Medium | 60 | 2 | 6 |
| **Authentication & Security** | Medium | 40 | 2 | 4 |
| **Reporting & Analytics Dashboard** | Medium | 50 | 2 | 5 |
| **Document Processing Pipeline** | High | 70 | 3 | 5 |
| **DevOps & Infrastructure** | Medium | 45 | 2 | 4 |
| **Testing (All Levels)** | Medium | 90 | 3 | 6 |
| **Project Management & BA** | Low | 80 | 2 | 8 |
| **Architecture & Technical Leadership** | Low | 60 | 2 | 8 |
| **TOTAL** | | **955** | | |

### Effort Distribution by Category

| Category | Person-Days | % of Total |
|----------|-----------|------------|
| Development | 480 | 50% |
| Testing & QA | 190 | 20% |
| Architecture & Design | 95 | 10% |
| DevOps & Infrastructure | 70 | 7% |
| Project Management & BA | 80 | 8% |
| Contingency Buffer (15%) | 140 | 15% |
| **Grand Total** | **1,055** | **100%** |

### Cost Model (Blended Rates)

| Resource Type | Rate (USD/Day) | Allocation | Estimated Cost |
|--------------|----------------|------------|---------------|
| Onshore Senior (Architect, PM) | $1,200 | 20% of effort | $253,200 |
| Onshore Mid-Level (Tech Lead, Sr. Dev) | $950 | 30% of effort | $300,675 |
| Offshore Senior (Sr. Dev, QA Lead) | $600 | 25% of effort | $158,250 |
| Offshore Mid-Level (Developer, QA) | $450 | 25% of effort | $118,688 |
| **Total Estimated Cost** | | | **$830,813** |

> **Note**: Rates are indicative and subject to final negotiation. Volume discounts and long-term engagement pricing may apply.

### Key Assumptions for Estimates

1. Requirements are 80% defined at project start; remaining 20% will be clarified during Discovery
2. Client will provide timely access to subject matter experts (within 48 hours of request)
3. Development and staging environments will be provisioned within the first 2 weeks
4. Third-party API documentation and sandbox access will be available before Sprint 3
5. Change requests beyond agreed scope will be estimated and approved separately
6. Team ramp-up period of 2 weeks is included in Phase 1 estimates";
    }
}
