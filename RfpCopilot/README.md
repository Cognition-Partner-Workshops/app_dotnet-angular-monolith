# RFP Copilot

A full-stack AI-powered application that accepts RFP/RFI documents and produces comprehensive, structured RFP responses using multi-agent orchestration.

## Architecture

- **Frontend**: Angular 17+ with Angular Material UI
- **Backend**: .NET 8 ASP.NET Core Web API
- **AI Framework**: Microsoft Semantic Kernel with multi-agent orchestration
- **Database**: SQLite (development) / SQL Server (production)
- **Real-time**: SignalR for live progress updates

## AI Agents

| Agent | Section | Description |
|-------|---------|-------------|
| TrackerAgent | Step 0 | RFP tracking, CRM validation, email notifications |
| SolutionApproachAgent | Step 1 | AI-First solution design and technology recommendations |
| EstimationAgent | Step 2 | Effort estimation, cost modeling, resource planning |
| CloudMigrationAgent | Step 3 | Cloud architecture and migration strategy (conditional) |
| IntegrationAgent | Step 4 | System integration patterns and API strategy |
| TestingDevOpsAgent | Step 5 | Testing strategy, CI/CD, and cutover planning |
| StaffingTimelineAgent | Step 6 | Team composition, timelines, and governance |
| RisksAssumptionsAgent | Step 7 | Risk register and assumptions documentation |

## Quick Start

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- npm

### Development Setup

```bash
# 1. Install Angular dependencies
cd client-app
npm install

# 2. Build Angular frontend
npx ng build

# 3. Run the .NET API (serves both API and Angular SPA)
cd ../src/RfpCopilot.Api
dotnet run
```

The application will be available at `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger`

### Docker

```bash
docker-compose up --build
```

## Configuration

Edit `src/RfpCopilot.Api/appsettings.json`:

```json
{
  "AzureOpenAI": {
    "Endpoint": "YOUR_CLAUDE_CODE_ENDPOINT_HERE",
    "ApiKey": "YOUR_CLAUDE_CODE_API_KEY_HERE",
    "DeploymentName": "YOUR_CLAUDE_CODE_DEPLOYMENT_NAME_HERE"
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "FromAddress": "rfpcopilot@company.com"
  }
}
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/rfp/upload` | Upload RFP document + metadata |
| GET | `/api/rfp/{id}/status` | Get processing status |
| GET | `/api/rfp/{id}/response` | Get complete response |
| POST | `/api/rfp/{id}/regenerate/{section}` | Regenerate a section |
| GET | `/api/tracker` | List all tracker entries |
| PUT | `/api/tracker/{rfpId}` | Update tracker entry |
| POST | `/api/tracker/export` | Export tracker to Excel |
| GET | `/api/response/{rfpId}/download/docx` | Download as DOCX |
| GET | `/api/response/{rfpId}/download/pdf` | Download as PDF |

## Project Structure

```
RfpCopilot/
├── src/
│   ├── RfpCopilot.Api/           # ASP.NET Core Web API
│   │   ├── Agents/               # AI agents (Orchestrator + 8 sub-agents)
│   │   ├── Controllers/          # API controllers
│   │   ├── Data/                 # EF Core DbContext, seed data
│   │   ├── Hubs/                 # SignalR hub for real-time updates
│   │   ├── Models/               # Domain models
│   │   ├── Prompts/              # Externalized prompt templates
│   │   └── Services/             # Business services
│   └── RfpCopilot.Api.Tests/     # xUnit tests
├── client-app/                   # Angular 17 SPA
│   └── src/app/
│       ├── features/             # Page components
│       │   ├── upload/           # RFP upload page
│       │   ├── tracker/          # RFP tracker dashboard
│       │   ├── response/         # Response viewer with tabs
│       │   └── settings/         # Configuration page
│       └── shared/               # Models, services
├── docker-compose.yml
├── Dockerfile
└── RfpCopilot.sln
```

## Running Tests

```bash
cd src/RfpCopilot.Api.Tests
dotnet test
```
