using System.Net;
using IntegrationTests.Infrastructure;
using Xunit;

namespace IntegrationTests.Controllers;

public class AuthControllerTests : IntegrationTestBase
{
    public AuthControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_BindsBodyAndRoutesToService()
    {
        AuthService.Reset();

        var payload = new
        {
            Email = "user@example.com",
            Password = "Super$ecret123"
        };

        var response = await Client.PostAsync("/api/Auth/login", JsonContent(payload));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, AuthService.AuthenticateCallCount);
        Assert.Equal(payload.Email, AuthService.LastEmail);
        Assert.Equal(payload.Password, AuthService.LastPassword);
    }

    [Fact]
    public async Task Logout_RoutesToService()
    {
        AuthService.Reset();

        var response = await Client.GetAsync("/api/Auth/logout");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(1, AuthService.LogoutCallCount);
    }
}
