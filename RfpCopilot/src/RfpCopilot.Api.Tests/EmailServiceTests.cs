using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RfpCopilot.Api.Services;
using Xunit;

namespace RfpCopilot.Api.Tests;

public class EmailServiceTests
{
    [Fact]
    public async Task SendEmailAsync_ShouldReturnTrue_WhenSmtpNotConfigured()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:SmtpHost"] = "localhost",
                ["Email:SmtpPort"] = "587",
                ["Email:FromAddress"] = "test@test.com"
            })
            .Build();

        var logger = new Mock<ILogger<EmailService>>();
        var service = new EmailService(config, logger.Object);

        var result = await service.SendEmailAsync("recipient@test.com", "Test Subject", "Test Body");

        Assert.True(result);
    }
}
