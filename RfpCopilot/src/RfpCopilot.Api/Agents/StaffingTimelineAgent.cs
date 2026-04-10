using Microsoft.SemanticKernel;
using RfpCopilot.Api.Models;

namespace RfpCopilot.Api.Agents;

public class StaffingTimelineAgent : BaseContentAgent
{
    protected override string AgentName => "StaffingTimelineAgent";
    protected override int SectionNumber => 6;
    protected override string SectionTitle => "Staffing & Timelines";
    protected override string PromptFileName => "staffing-timeline-prompt.txt";

    public StaffingTimelineAgent(Kernel kernel, ILogger<StaffingTimelineAgent> logger) : base(kernel, logger) { }

    protected override string GetFallbackContent(AgentTask task)
    {
        var client = task.ClientName ?? "the Client";
        return $@"## Staffing & Timelines

### Project Timeline Overview

The project for {client} is planned across **6 phases over 38 weeks** (approximately 9.5 months), with defined milestones and deliverables at each stage.

| Phase | Duration | Start (Week) | End (Week) | Key Milestone |
|-------|----------|-------------|-----------|---------------|
| **Phase 1: Discovery & Planning** | 4 weeks | W1 | W4 | Project Charter, Architecture Blueprint approved |
| **Phase 2: Design** | 6 weeks | W5 | W10 | Design Specifications signed off |
| **Phase 3: Build - Sprint 1-6** | 12 weeks | W11 | W22 | Core platform MVP ready |
| **Phase 4: Build - Sprint 7-10** | 8 weeks | W23 | W30 | Feature-complete release candidate |
| **Phase 5: Test & Harden** | 4 weeks | W31 | W34 | UAT sign-off, production readiness |
| **Phase 6: Deploy & Hypercare** | 4 weeks | W35 | W38 | Go-live, transition to support |

### Team Composition

| Role | Count | Onshore/Offshore | Phase(s) | Key Responsibilities |
|------|-------|-----------------|----------|---------------------|
| **Project Manager** | 1 | Onshore | All (W1-W38) | Overall delivery, stakeholder management, risk management, status reporting |
| **Solution Architect** | 1 | Onshore | P1-P4 (W1-W30) | Architecture design, tech decisions, code reviews, POC leadership |
| **Tech Lead (Backend)** | 1 | Onshore | P2-P5 (W5-W34) | .NET development leadership, API design, agent orchestration |
| **Tech Lead (Frontend)** | 1 | Offshore | P2-P5 (W5-W34) | Angular SPA development leadership, UX implementation |
| **Senior Developer** | 3 | 1 Onshore / 2 Offshore | P3-P4 (W11-W30) | Core feature development, AI/ML integration, complex modules |
| **Developer** | 4 | Offshore | P3-P4 (W11-W30) | Feature development, bug fixes, unit testing |
| **AI/ML Engineer** | 2 | 1 Onshore / 1 Offshore | P2-P5 (W5-W34) | LLM integration, prompt engineering, agent development, model tuning |
| **QA Lead** | 1 | Onshore | P3-P6 (W11-W38) | Test strategy, test plan, automation framework, UAT coordination |
| **QA Engineer** | 3 | Offshore | P3-P5 (W11-W34) | Test case execution, automation scripts, regression testing |
| **DevOps Engineer** | 1 | Offshore | P2-P6 (W5-W38) | CI/CD pipeline, infrastructure, cloud provisioning, monitoring |
| **Business Analyst** | 1 | Onshore | P1-P3 (W1-W22) | Requirements refinement, user stories, acceptance criteria |
| **Scrum Master** | 1 | Offshore | P3-P5 (W11-W34) | Sprint facilitation, impediment removal, agile coaching |
| **UX Designer** | 1 | Offshore | P1-P3 (W1-W22) | User research, wireframes, visual design, design system |
| **Data Engineer** | 1 | Offshore | P3-P4 (W11-W30) | Data migration, ETL pipelines, database optimization |
| **TOTAL** | **22** | **5 Onshore / 17 Offshore** | | |

### Ramp-Up & Ramp-Down Plan

| Week | Team Size | Key Changes |
|------|----------|-------------|
| W1-W4 | 6 | Core team: PM, Architect, BA, UX Designer, DevOps, AI/ML Lead |
| W5-W10 | 10 | Add: Tech Leads (2), AI/ML Engineer, QA Lead |
| W11-W14 | 18 | Add: Developers (7), QA Engineers (3), Scrum Master, Data Engineer |
| W15-W22 | 22 | Full team — peak delivery capacity |
| W23-W30 | 20 | Ramp down: BA, UX Designer transition off |
| W31-W34 | 15 | Ramp down: 4 Developers, 1 QA Engineer transition off |
| W35-W38 | 8 | Hypercare team: PM, Architect, 2 Devs, 2 QA, DevOps, AI/ML |

### Key Milestones & Deliverables

| Milestone | Target Date (Week) | Deliverables | Approval Required |
|-----------|-------------------|--------------|-------------------|
| Project Kickoff | W1 | Project Charter, Communication Plan | PM, Client Sponsor |
| Architecture Approved | W4 | Solution Architecture Document, Tech Stack Decision | Solution Architect, Client CTO |
| Design Complete | W10 | UI/UX Designs, API Contracts, Data Model, Security Design | All Leads, Client BA |
| Sprint 3 Demo (MVP) | W16 | Working MVP with core user flows | Product Owner |
| Sprint 6 Demo | W22 | Complete core platform with AI agents | Product Owner, Client |
| Feature Freeze | W30 | Feature-complete application, regression passed | PM, QA Lead |
| UAT Sign-off | W34 | UAT completion report, 0 critical/high bugs | Client Business Users |
| Go-Live | W35 | Production deployment, cutover complete | Steering Committee |
| Hypercare Complete | W38 | Transition to BAU support, lessons learned | PM, Client Sponsor |

### Governance Model

- **Steering Committee**: Monthly meetings — executive sponsors, PM, Solution Architect
- **Weekly Status Report**: Progress against milestones, risks, issues, budget burn
- **Sprint Review/Demo**: Bi-weekly sprint demos with client stakeholders
- **Daily Standups**: 15-minute daily sync for development team
- **Risk Review**: Bi-weekly risk assessment and mitigation tracking
- **Change Control Board**: As needed for scope change requests";
    }
}
