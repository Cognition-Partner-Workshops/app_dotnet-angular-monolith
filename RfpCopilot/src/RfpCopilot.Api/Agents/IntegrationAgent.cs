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

    protected override string GetFallbackContent(AgentTask task)
    {
        var client = task.ClientName ?? "the Client";
        return $@"## Integration Strategy

### Integration Inventory

Based on analysis of the RFP from {client}, the following system integrations have been identified:

| # | System | Direction | Protocol | Data Format | Frequency | SLA |
|---|--------|-----------|----------|-------------|-----------|-----|
| 1 | **CRM System** (Salesforce/Dynamics) | Bidirectional | REST API | JSON | Real-time | 99.9% |
| 2 | **ERP System** (SAP/Oracle) | Inbound | OData / RFC | JSON/XML | Batch (hourly) | 99.5% |
| 3 | **Identity Provider** (Azure AD/Okta) | Inbound | SAML 2.0 / OIDC | JWT | Real-time | 99.99% |
| 4 | **Email Service** (Exchange/SendGrid) | Outbound | SMTP / REST | MIME/JSON | Event-driven | 99.5% |
| 5 | **Document Management** (SharePoint) | Bidirectional | Graph API | JSON | Real-time | 99.9% |
| 6 | **Payment Gateway** (Stripe/PayPal) | Outbound | REST API | JSON | Real-time | 99.99% |
| 7 | **Analytics Platform** (Power BI) | Outbound | REST API | JSON/OData | Scheduled (daily) | 99.5% |
| 8 | **External Data Provider** | Inbound | REST/SFTP | CSV/JSON | Batch (daily) | 99.0% |
| 9 | **Notification Hub** (Push/SMS) | Outbound | REST API | JSON | Event-driven | 99.5% |
| 10 | **Legacy System** (Mainframe) | Inbound | MQ/SFTP | Fixed-width/XML | Batch (nightly) | 99.0% |

### Integration Patterns

We recommend a layered integration architecture using the following patterns:

1. **API-Based (REST/GraphQL)**: For real-time, synchronous integrations with modern systems
2. **Event-Driven (Message Bus)**: For asynchronous, decoupled communication between services
3. **File-Based (SFTP/Blob)**: For batch integrations with legacy systems
4. **Database-Level (CDC)**: For near-real-time data synchronization with minimal application changes

### API Gateway & API Management

- **Centralized API Gateway** for all inbound and outbound API traffic
- **Rate limiting** and throttling to protect backend services
- **API versioning** (URL path-based) for backward compatibility
- **Request/Response transformation** for format normalization
- **API analytics** and usage monitoring dashboards
- **Developer portal** with self-service API documentation

### Error Handling & Resilience

| Pattern | Implementation | Use Case |
|---------|---------------|----------|
| **Retry with Exponential Backoff** | Polly library (.NET) | Transient failures (network timeouts, 503s) |
| **Circuit Breaker** | Polly CircuitBreaker | Cascading failure prevention |
| **Dead Letter Queue** | Azure Service Bus DLQ | Failed message preservation for investigation |
| **Idempotency** | Unique correlation IDs | Preventing duplicate processing on retry |
| **Bulkhead Isolation** | Thread pool partitioning | Preventing one integration from starving others |
| **Timeout Policy** | HttpClient timeout + CancellationToken | Preventing indefinite waits |

### Security

- **OAuth 2.0 / OpenID Connect**: For API authentication and authorization
- **mTLS (Mutual TLS)**: For service-to-service communication
- **API Keys**: For third-party integrations with rate-limited access
- **Data Encryption**: TLS 1.3 in transit, AES-256 at rest
- **Secret Management**: Azure Key Vault for credentials, connection strings, certificates
- **IP Whitelisting**: For legacy system and partner network access

### Integration Testing Approach

- **Contract Testing**: Pact-based consumer-driven contract tests for API compatibility
- **Mock Services**: WireMock for simulating external systems in dev/test environments
- **End-to-End Integration Tests**: Automated test suites running against staging environment
- **Chaos Engineering**: Simulating integration failures to validate resilience patterns
- **Performance Testing**: Load testing integration endpoints to verify SLA compliance";
    }
}
