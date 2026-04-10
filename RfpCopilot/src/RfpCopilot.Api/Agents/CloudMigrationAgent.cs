using Microsoft.SemanticKernel;
using RfpCopilot.Api.Models;

namespace RfpCopilot.Api.Agents;

public class CloudMigrationAgent : BaseContentAgent
{
    protected override string AgentName => "CloudMigrationAgent";
    protected override int SectionNumber => 3;
    protected override string SectionTitle => "Architecture Modernization & Cloud Migration";
    protected override string PromptFileName => "cloud-migration-prompt.txt";

    public CloudMigrationAgent(Kernel kernel, ILogger<CloudMigrationAgent> logger) : base(kernel, logger) { }

    public override async Task<AgentResult> ExecuteAsync(AgentTask task)
    {
        if (!task.IsCloudMigrationInScope)
        {
            Logger.LogInformation("Cloud migration not in scope, skipping CloudMigrationAgent");
            return new AgentResult
            {
                AgentName = AgentName,
                SectionNumber = SectionNumber,
                SectionTitle = SectionTitle,
                Content = "## Architecture Modernization & Cloud Migration\n\n> **Note:** Cloud migration is not in scope per client direction. This section has been intentionally omitted from the response.\n\nIf cloud migration becomes relevant in the future, we can provide a comprehensive migration strategy covering current-state assessment, target architecture design, 6R migration framework application, data migration planning, and cloud cost optimization.",
                Success = true,
                TokensUsed = 0,
                CompletedAt = DateTime.UtcNow
            };
        }

        return await base.ExecuteAsync(task);
    }

    protected override string PreparePrompt(AgentTask task)
    {
        var provider = task.PreferredCloudProvider ?? "Azure";
        return SystemPrompt.Replace("{{CLOUD_PROVIDER}}", provider);
    }

    protected override string GetFallbackContent(AgentTask task)
    {
        var client = task.ClientName ?? "the Client";
        var provider = task.PreferredCloudProvider ?? "Azure";
        return $@"## Architecture Modernization & Cloud Migration

### Current-State Architecture Assessment

Based on our analysis of {client}'s RFP, the current architecture consists of legacy on-premises components that require modernization to achieve scalability, resilience, and cost optimization through cloud-native services.

**Current Architecture Characteristics:**
- Monolithic application architecture with tightly coupled components
- On-premises infrastructure with limited horizontal scalability
- Manual deployment processes with long release cycles
- Traditional relational databases with potential performance bottlenecks
- Limited observability and monitoring capabilities

### Target Cloud Architecture ({provider})

Our recommended target architecture leverages **{provider}** managed services to maximize operational efficiency and minimize infrastructure management overhead.

| Current Component | Target Cloud Service ({provider}) | Migration Strategy (6R) | Priority |
|------------------|----------------------------------|------------------------|----------|
| **Web Application Server** | {(provider == "Azure" ? "Azure App Service / AKS" : provider == "AWS" ? "AWS ECS / EKS" : "Google Cloud Run / GKE")} | Refactor | P1 - Critical |
| **Application Database** | {(provider == "Azure" ? "Azure SQL Database" : provider == "AWS" ? "Amazon RDS / Aurora" : "Cloud SQL")} | Replatform | P1 - Critical |
| **File Storage** | {(provider == "Azure" ? "Azure Blob Storage" : provider == "AWS" ? "Amazon S3" : "Cloud Storage")} | Replatform | P1 - Critical |
| **Message Queue** | {(provider == "Azure" ? "Azure Service Bus" : provider == "AWS" ? "Amazon SQS/SNS" : "Cloud Pub/Sub")} | Refactor | P2 - High |
| **Cache Layer** | {(provider == "Azure" ? "Azure Redis Cache" : provider == "AWS" ? "Amazon ElastiCache" : "Memorystore")} | Replatform | P2 - High |
| **Identity & Access** | {(provider == "Azure" ? "Azure AD / Entra ID" : provider == "AWS" ? "AWS Cognito / IAM" : "Cloud Identity")} | Refactor | P1 - Critical |
| **API Gateway** | {(provider == "Azure" ? "Azure API Management" : provider == "AWS" ? "Amazon API Gateway" : "Apigee")} | Refactor | P2 - High |
| **Search Service** | {(provider == "Azure" ? "Azure AI Search" : provider == "AWS" ? "Amazon OpenSearch" : "Cloud Search")} | Repurchase | P3 - Medium |
| **Monitoring** | {(provider == "Azure" ? "Azure Monitor / App Insights" : provider == "AWS" ? "CloudWatch / X-Ray" : "Cloud Operations Suite")} | Repurchase | P2 - High |
| **CI/CD Pipeline** | {(provider == "Azure" ? "Azure DevOps / GitHub Actions" : provider == "AWS" ? "AWS CodePipeline" : "Cloud Build")} | Replatform | P2 - High |
| **Legacy Batch Jobs** | {(provider == "Azure" ? "Azure Functions" : provider == "AWS" ? "AWS Lambda" : "Cloud Functions")} | Refactor | P3 - Medium |
| **Report Server** | {(provider == "Azure" ? "Power BI Embedded" : provider == "AWS" ? "Amazon QuickSight" : "Looker")} | Repurchase | P3 - Medium |

### Data Migration Approach

**Strategy**: Phased migration using a combination of ETL, Change Data Capture (CDC), and bulk transfer.

| Phase | Data Type | Approach | Downtime | Duration |
|-------|----------|----------|----------|----------|
| Phase 1 | Reference Data | Bulk ETL (one-time) | None | 1 week |
| Phase 2 | Transactional Data | CDC with dual-write | Minimal (<1 hr) | 2 weeks |
| Phase 3 | Historical Data | Batch ETL (background) | None | 3 weeks |
| Phase 4 | File/Blob Data | {(provider == "Azure" ? "AzCopy" : provider == "AWS" ? "AWS DataSync" : "gsutil")} parallel transfer | None | 1 week |
| Phase 5 | Validation & Cutover | Automated comparison scripts | 2-4 hrs | 1 week |

### Cloud Cost Estimation (Monthly)

| Service Category | Estimated Monthly Cost | Notes |
|-----------------|----------------------|-------|
| Compute (App Hosting) | $2,800 | Auto-scaling, reserved instances |
| Database (Managed SQL) | $1,200 | Business Critical tier |
| Storage (Blob/Files) | $400 | Hot + Cool tiers |
| Networking (Load Balancer, CDN) | $600 | Global distribution |
| AI/ML Services | $1,500 | Azure OpenAI consumption |
| Monitoring & Security | $500 | Full observability stack |
| DevOps & CI/CD | $300 | Pipeline minutes |
| **Total Estimated Monthly** | **$7,300** | |
| **Annual Cloud Cost** | **$87,600** | |
| **Current On-Prem Annual Cost** | **~$150,000** | Hardware, licenses, staffing |
| **Estimated Annual Savings** | **~$62,400 (42%)** | |

### Migration Timeline & Wave Planning

| Wave | Duration | Components | Risk Level |
|------|----------|-----------|------------|
| **Wave 1** (Foundation) | Weeks 1-4 | Landing zone, networking, IAM, CI/CD pipeline | Low |
| **Wave 2** (Core Services) | Weeks 5-10 | Database migration, application refactoring, cache layer | Medium |
| **Wave 3** (Integration) | Weeks 11-14 | API Gateway, message queues, external integrations | Medium |
| **Wave 4** (Advanced) | Weeks 15-18 | AI/ML services, search, analytics, reporting | Low |
| **Wave 5** (Cutover) | Weeks 19-20 | Final data sync, DNS cutover, monitoring validation | High |";
    }
}
