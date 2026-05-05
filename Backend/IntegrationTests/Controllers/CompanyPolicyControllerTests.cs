using System.Net;
using IntegrationTests.Infrastructure;
using Xunit;

namespace IntegrationTests.Controllers;

public class CompanyPolicyControllerTests : IntegrationTestBase
{
    public CompanyPolicyControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetHoliday_BindsRoute()
    {
        var response = await Client.GetAsync("/api/CompanyPolicy/holidays");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddHolidays_BindsBody()
    {
        var payload = new
        {
            Holidays = new[]
            {
                new DateTime(2026, 12, 24),
                new DateTime(2026, 12, 31)
            }
        };

        var response = await Client.PostAsync("/api/CompanyPolicy/holidays", JsonContent(payload));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(CompanyPolicyService.LastHolidays);
        Assert.Equal(2, CompanyPolicyService.LastHolidays?.Holidays.Count);
    }

    [Fact]
    public async Task RemoveHolidays_BindsBody()
    {
        var payload = new
        {
            Holidays = new[]
            {
                new DateTime(2026, 1, 1)
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/CompanyPolicy/holidays")
        {
            Content = JsonContent(payload)
        };

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(CompanyPolicyService.LastHolidays);
        Assert.Single(CompanyPolicyService.LastHolidays?.Holidays ?? new List<DateTime>());
    }

    [Fact]
    public async Task SetPolicy_BindsBody()
    {
        var payload = new
        {
            MinimumRestPeriodInMinutes = 30,
            RestPeriodThresholdInMinutes = 60,
            MaximumConsecutiveWorkHoursPerDay = 8,
            MaximumConsecutiveWorkHoursPerWeek = 40
        };

        var response = await Client.PutAsync("/api/CompanyPolicy", JsonContent(payload));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(CompanyPolicyService.LastSetPolicy);
        Assert.Equal(payload.MinimumRestPeriodInMinutes, CompanyPolicyService.LastSetPolicy?.MinimumRestPeriodInMinutes);
        Assert.Equal(payload.RestPeriodThresholdInMinutes, CompanyPolicyService.LastSetPolicy?.RestPeriodThresholdInMinutes);
        Assert.Equal(payload.MaximumConsecutiveWorkHoursPerDay, CompanyPolicyService.LastSetPolicy?.MaximumConsecutiveWorkHoursPerDay);
        Assert.Equal(payload.MaximumConsecutiveWorkHoursPerWeek, CompanyPolicyService.LastSetPolicy?.MaximumConsecutiveWorkHoursPerWeek);
    }

    [Fact]
    public async Task GetPolicy_BindsRoute()
    {
        var response = await Client.GetAsync("/api/CompanyPolicy");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
