using System.Net;
using IntegrationTests.Infrastructure;
using Schichtpilot.Models.Enums;
using Schichtpilot.Models.Responses;
using Xunit;

namespace IntegrationTests.Middleware;

public class MiddlewareTests : IntegrationTestBase
{
    public MiddlewareTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ValidationFilter_Returns_BadRequest_With_ErrorResponse()
    {
        var payload = new { };

        var response = await Client.PostAsync("/api/User", JsonContent(payload));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorResponse = await ReadJsonAsync<ErrorResponse>(response);
        Assert.NotNull(errorResponse);
        Assert.NotEmpty(errorResponse!.ErrorStates);
    }

    [Fact]
    public async Task UserContextMiddleware_Sets_UserId_For_Authenticated_Request()
    {
        var payload = new
        {
            StartDate = DateTime.UtcNow.Date.AddDays(2),
            EndDate = DateTime.UtcNow.Date.AddDays(3),
            AbsenceType = nameof(AbsenceTypeEnum.Vacation),
            Message = "Integration test"
        };

        var response = await Client.PostAsync("/api/Absence", JsonContent(payload));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(1L, AbsenceService.LastCreateUserId);
    }
}
