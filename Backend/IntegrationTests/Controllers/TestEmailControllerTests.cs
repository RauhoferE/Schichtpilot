using System.Net;
using IntegrationTests.Infrastructure;
using Xunit;

namespace IntegrationTests.Controllers;

public class TestEmailControllerTests : IntegrationTestBase
{
    public TestEmailControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ApprovalEmail_BindsQueryEmail()
    {
        EmailService.ApprovalEmails.Clear();
        var targetEmail = "manager@example.com";

        var response = await Client.PostAsync($"/api/test-email/approval?toEmail={targetEmail}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(EmailService.ApprovalEmails);
        Assert.Equal(targetEmail, EmailService.ApprovalEmails[0].Employee.Email);
    }
}
