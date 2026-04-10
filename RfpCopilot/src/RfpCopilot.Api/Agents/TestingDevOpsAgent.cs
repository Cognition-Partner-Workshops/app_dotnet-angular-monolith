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

    protected override string GetFallbackContent(AgentTask task)
    {
        return @"## Testing, DevOps & Cutover Strategy

### Testing Strategy

#### Test Pyramid

Our testing approach follows the industry-standard **test pyramid** to ensure comprehensive coverage with optimal execution time:

| Test Level | Coverage Target | Tools | Execution |
|-----------|----------------|-------|-----------|
| **Unit Tests** | 80%+ code coverage | xUnit (.NET), Jest (Angular) | Every commit (CI) |
| **Integration Tests** | All API endpoints, DB operations | xUnit + TestContainers | Every PR merge |
| **E2E Tests** | Critical user journeys (top 20 flows) | Playwright / Cypress | Nightly + Pre-release |
| **Performance Tests** | Load, stress, endurance | k6 / JMeter | Weekly + Pre-release |
| **Security Tests** | OWASP Top 10, SAST/DAST | SonarQube, OWASP ZAP | Weekly + Pre-release |
| **AI Model Tests** | Prompt quality, hallucination detection | Custom framework | Per model update |

#### Test Automation Framework

- **Backend**: xUnit with Moq for mocking, FluentAssertions for readable assertions, Bogus for test data generation
- **Frontend**: Jest for unit tests, Playwright for E2E browser tests, Angular Testing Library for component tests
- **API Testing**: REST Assured patterns with xUnit, automated contract testing with Pact
- **Performance**: k6 scripts in JavaScript for load testing, integrated into CI pipeline

#### Test Environment Strategy

| Environment | Purpose | Data | Refresh Cycle |
|------------|---------|------|---------------|
| **Dev** | Developer testing | Synthetic (seeded) | On-demand |
| **QA** | Functional testing, automation | Anonymized production subset | Weekly |
| **Staging** | UAT, pre-production validation | Production mirror (anonymized) | Bi-weekly |
| **Performance** | Load/stress testing | Scaled synthetic data | Before each test cycle |

#### UAT & Regression Testing

- **UAT Duration**: 3 weeks with dedicated business user participation
- **Test Scenarios**: 100+ UAT scenarios covering all business workflows
- **Regression Suite**: Automated regression of 500+ test cases run nightly
- **Defect Triage**: Daily defect triage meetings during UAT with severity-based SLAs

#### Entry/Exit Criteria

| Phase | Entry Criteria | Exit Criteria |
|-------|---------------|---------------|
| **Unit Testing** | Code complete, peer reviewed | 80%+ coverage, 0 critical bugs |
| **Integration Testing** | Unit tests passing, APIs deployed | All integration points verified, 0 blocker bugs |
| **System Testing** | Integration tests passing | All test cases executed, <5 medium bugs open |
| **UAT** | System testing complete | Business sign-off, 0 critical/high bugs |
| **Performance** | Functional testing complete | Response times within SLA, no memory leaks |

---

### DevOps Strategy

#### CI/CD Pipeline Design

**Pipeline Stages:**
1. **Source** → Git push triggers pipeline (GitHub Actions / Azure DevOps)
2. **Build** → Compile .NET backend + Angular frontend (parallel)
3. **Test** → Unit tests + SAST scan (SonarQube)
4. **Package** → Docker image build + push to container registry
5. **Deploy to Dev** → Automatic deployment to dev environment
6. **Integration Tests** → Run integration test suite
7. **Deploy to QA** → Automatic deployment (with approval gate for Staging)
8. **Deploy to Staging** → Manual approval required
9. **Deploy to Production** → Manual approval + Go/No-Go checklist

#### Branching Strategy (Trunk-Based Development)

- **main**: Production-ready code, protected branch
- **feature/***: Short-lived feature branches (max 2-3 days)
- **release/***: Release branches for hotfix isolation
- **Pull Request workflow**: Required code review (2 approvers), passing CI, no merge conflicts

#### Infrastructure as Code

- **Terraform**: All cloud infrastructure defined as code in version-controlled modules
- **Helm Charts**: Kubernetes deployment manifests for all microservices
- **Environment Parity**: Dev, QA, Staging, and Production use identical IaC templates with environment-specific variables

#### Monitoring & Observability

| Pillar | Tool | Purpose |
|--------|------|---------|
| **Metrics** | Prometheus + Grafana | System and application metrics, dashboards, alerting |
| **Logs** | ELK Stack (Elasticsearch, Logstash, Kibana) | Centralized log aggregation and search |
| **Traces** | OpenTelemetry + Jaeger | Distributed tracing across microservices |
| **APM** | Azure Application Insights | Application performance monitoring, dependency mapping |
| **Alerting** | PagerDuty + Grafana Alerts | Incident notification and escalation |

#### SRE Practices

- **SLOs**: 99.9% availability, <200ms P95 API response time, <1% error rate
- **Error Budgets**: 0.1% downtime budget per month (~43 minutes), tracked via dashboards
- **Incident Response**: PagerDuty escalation → P1 response in 15 min, P2 in 30 min

---

### Cutover Strategy

#### Approach: Blue-Green Deployment with Canary Release

1. **Blue Environment**: Current production (existing system)
2. **Green Environment**: New system deployed and validated
3. **Canary Phase**: Route 5% → 10% → 25% → 50% → 100% of traffic to Green
4. **Rollback**: Instant rollback by routing traffic back to Blue

#### Cutover Checklist

- [ ] All test phases completed with exit criteria met
- [ ] Data migration validated (row counts, checksums, sampling)
- [ ] Performance benchmarks met in staging environment
- [ ] Security audit completed and findings remediated
- [ ] Runbook reviewed and approved by operations team
- [ ] Communication sent to all stakeholders
- [ ] Support team trained and on standby
- [ ] Monitoring dashboards configured and alerting enabled
- [ ] DNS TTL reduced 48 hours before cutover
- [ ] Rollback procedure tested and documented

#### Go/No-Go Criteria

| Category | Go Criteria | No-Go Criteria |
|----------|-----------|---------------|
| **Functionality** | All critical features tested and working | Any P1/P2 bug unresolved |
| **Performance** | Meets SLA under expected load | Response times exceed thresholds |
| **Security** | No critical/high vulnerabilities open | Unresolved security findings |
| **Data** | Migration validated, reconciliation complete | Data discrepancies found |
| **Operations** | Runbook approved, team trained | Operations team not ready |

#### Hypercare Plan

- **Duration**: 4 weeks post-go-live
- **Team**: Dedicated hypercare team (8 members) — 2 developers, 2 QA, 1 DevOps, 1 BA, 1 PM, 1 architect
- **Coverage**: 24/7 support for Week 1-2, business hours for Week 3-4
- **Escalation Matrix**: L1 (Support) → L2 (Dev Team) → L3 (Architecture) → Steering Committee";
    }
}
