using System.Net;
using System.Net.Mail;

namespace RfpCopilot.Api.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var smtpHost = _configuration["Email:SmtpHost"] ?? "localhost";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUser = _configuration["Email:SmtpUser"] ?? "";
            var smtpPassword = _configuration["Email:SmtpPassword"] ?? "";
            var fromAddress = _configuration["Email:FromAddress"] ?? "rfpcopilot@company.com";

            _logger.LogInformation("Sending email to {To} with subject '{Subject}'", to, subject);

            // In development/demo mode, log the email instead of sending
            if (string.IsNullOrEmpty(smtpHost) || smtpHost == "localhost")
            {
                _logger.LogWarning("SMTP not configured. Email would be sent to: {To}, Subject: {Subject}", to, subject);
                _logger.LogInformation("Email Body:\n{Body}", body);
                return true;
            }

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage(fromAddress, to, subject, body)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {To}", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return false;
        }
    }
}
