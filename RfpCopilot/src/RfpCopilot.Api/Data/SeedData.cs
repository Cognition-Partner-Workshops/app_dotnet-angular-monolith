using RfpCopilot.Api.Models;

namespace RfpCopilot.Api.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext context)
    {
        context.Database.EnsureCreated();

        if (context.RfpTrackerEntries.Any())
            return;

        var entries = new List<RfpTrackerEntry>
        {
            new()
            {
                RfpId = "RFP-2025-001", RfpTitle = "Enterprise ERP Modernization", ClientName = "Acme Corp",
                CrmId = "CRM-2025-001", OriginatorName = "John Smith", OriginatorEmail = "abc_xyz@gmail.com",
                ReceivedDate = DateTime.UtcNow.AddDays(-30), DueDate = DateTime.UtcNow.AddDays(30),
                Status = "In Progress", AssignedTo = "Team Alpha", Priority = "High", Notes = "Large enterprise deal"
            },
            new()
            {
                RfpId = "RFP-2025-002", RfpTitle = "Cloud Migration Assessment", ClientName = "TechStart Inc",
                CrmId = "CRM-2025-002", OriginatorName = "Jane Doe", OriginatorEmail = "abc_xyz@gmail.com",
                ReceivedDate = DateTime.UtcNow.AddDays(-25), DueDate = DateTime.UtcNow.AddDays(20),
                Status = "New", AssignedTo = "Team Beta", Priority = "Medium", Notes = "Mid-market opportunity"
            },
            new()
            {
                RfpId = "RFP-2025-003", RfpTitle = "Digital Transformation Platform", ClientName = "Global Finance Ltd",
                CrmId = null, OriginatorName = "Bob Wilson", OriginatorEmail = "abc_xyz@gmail.com",
                ReceivedDate = DateTime.UtcNow.AddDays(-20), DueDate = DateTime.UtcNow.AddDays(25),
                Status = "Pending CRM", AssignedTo = null, Priority = "High",
                Notes = "Missing CRM ID - awaiting response", EmailSentForMissingCrm = true, EmailSentAt = DateTime.UtcNow.AddDays(-19)
            },
            new()
            {
                RfpId = "RFP-2025-004", RfpTitle = "AI-Powered Customer Service", ClientName = "RetailMax",
                CrmId = "CRM-2025-004", OriginatorName = "Alice Brown", OriginatorEmail = "abc_xyz@gmail.com",
                ReceivedDate = DateTime.UtcNow.AddDays(-15), DueDate = DateTime.UtcNow.AddDays(35),
                Status = "In Progress", AssignedTo = "Team Gamma", Priority = "Medium", Notes = "AI/ML focused"
            },
            new()
            {
                RfpId = "RFP-2025-005", RfpTitle = "Healthcare Data Analytics", ClientName = "MedCare Systems",
                CrmId = null, OriginatorName = "Charlie Davis", OriginatorEmail = "abc_xyz@gmail.com",
                ReceivedDate = DateTime.UtcNow.AddDays(-10), DueDate = DateTime.UtcNow.AddDays(40),
                Status = "Pending CRM", AssignedTo = null, Priority = "Low",
                Notes = "Missing CRM ID - email sent", EmailSentForMissingCrm = true, EmailSentAt = DateTime.UtcNow.AddDays(-9)
            },
            new()
            {
                RfpId = "RFP-2025-006", RfpTitle = "Supply Chain Optimization", ClientName = "LogiTech Solutions",
                CrmId = "CRM-2025-006", OriginatorName = "Diana Evans", OriginatorEmail = "abc_xyz@gmail.com",
                ReceivedDate = DateTime.UtcNow.AddDays(-8), DueDate = DateTime.UtcNow.AddDays(45),
                Status = "New", AssignedTo = "Team Alpha", Priority = "High", Notes = "Strategic account"
            },
            new()
            {
                RfpId = "RFP-2025-007", RfpTitle = "Cybersecurity Assessment", ClientName = "SecureBank Corp",
                CrmId = null, OriginatorName = "Edward Fox", OriginatorEmail = "abc_xyz@gmail.com",
                ReceivedDate = DateTime.UtcNow.AddDays(-5), DueDate = DateTime.UtcNow.AddDays(15),
                Status = "Pending CRM", AssignedTo = null, Priority = "High",
                Notes = "Urgent - missing CRM ID", EmailSentForMissingCrm = true, EmailSentAt = DateTime.UtcNow.AddDays(-4)
            },
            new()
            {
                RfpId = "RFP-2025-008", RfpTitle = "Mobile App Development", ClientName = "AppWorld Inc",
                CrmId = "CRM-2025-008", OriginatorName = "Grace Hill", OriginatorEmail = "abc_xyz@gmail.com",
                ReceivedDate = DateTime.UtcNow.AddDays(-3), DueDate = DateTime.UtcNow.AddDays(50),
                Status = "New", AssignedTo = "Team Delta", Priority = "Medium", Notes = "Cross-platform requirement"
            },
            new()
            {
                RfpId = "RFP-2025-009", RfpTitle = "Data Warehouse Migration", ClientName = "DataDriven LLC",
                CrmId = null, OriginatorName = "Ivan Jones", OriginatorEmail = "abc_xyz@gmail.com",
                ReceivedDate = DateTime.UtcNow.AddDays(-2), DueDate = DateTime.UtcNow.AddDays(60),
                Status = "Pending CRM", AssignedTo = null, Priority = "Low",
                Notes = "Missing CRM ID", EmailSentForMissingCrm = false
            },
            new()
            {
                RfpId = "RFP-2025-010", RfpTitle = "IoT Platform Development", ClientName = "SmartFactory Corp",
                CrmId = null, OriginatorName = "Karen Lee", OriginatorEmail = "abc_xyz@gmail.com",
                ReceivedDate = DateTime.UtcNow.AddDays(-1), DueDate = DateTime.UtcNow.AddDays(55),
                Status = "Pending CRM", AssignedTo = null, Priority = "Medium",
                Notes = "New prospect - missing CRM ID", EmailSentForMissingCrm = false
            }
        };

        context.RfpTrackerEntries.AddRange(entries);
        context.SaveChanges();
    }
}
