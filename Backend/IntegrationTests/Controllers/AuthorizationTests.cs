using System.Net;
using IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace IntegrationTests.Controllers;

public class AuthorizationTests
{
    [Fact]
    public async Task Unauthenticated_Request_To_Authorized_Endpoint_Returns_401()
    {
        using var factory = new UnauthenticatedWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/api/Absence/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UserRole_Request_To_Admin_Endpoint_Returns_403()
    {
        using var factory = new UserOnlyWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/api/User/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminRole_Request_To_Admin_Endpoint_Returns_200()
    {
        using var factory = new AdminOnlyWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/api/User/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UserRole_Request_To_User_Endpoint_Returns_200()
    {
        using var factory = new UserOnlyWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var date = DateTime.UtcNow.AddDays(1).ToString("O");
        var response = await client.GetAsync($"/api/Workschedule/active?startDate={Uri.EscapeDataString(date)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
