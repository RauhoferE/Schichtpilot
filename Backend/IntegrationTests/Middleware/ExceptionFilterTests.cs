using System.Net;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace IntegrationTests.Middleware;

public class ExceptionFilterTests
{
    [Fact]
    public async Task ExceptionFilter_Returns_NotFound_Payload_For_NotFoundException()
    {
        using var factory = new ExceptionFilterWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/api/Absence/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);

        var statusCode = document.RootElement.GetProperty("statusCode").GetInt32();
        var message = document.RootElement.GetProperty("message").GetString();

        Assert.Equal(404, statusCode);
        Assert.Equal("Absence with id 999 was not found.", message);
    }
}
