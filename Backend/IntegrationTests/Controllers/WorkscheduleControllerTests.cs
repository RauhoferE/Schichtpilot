using System.Net;
using IntegrationTests.Infrastructure;
using Xunit;

namespace IntegrationTests.Controllers;

public class WorkscheduleControllerTests : IntegrationTestBase
{
    public WorkscheduleControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GenerateSchedule_BindsBodyAndRoutes()
    {
        var payload = new
        {
            Name = "Week 10",
            StartDate = DateTime.UtcNow.AddDays(2),
            EndDate = DateTime.UtcNow.AddDays(7),
            ShiftIds = new[] { 1, 2, 3 }
        };

        var response = await Client.PostAsync("/api/Workschedule/generate", JsonContent(payload));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ReGenerateSchedule_BindsRoute()
    {
        var response = await Client.PostAsync("/api/Workschedule/generate/42", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PublishSchedule_BindsRoute()
    {
        var response = await Client.PostAsync("/api/Workschedule/publish/7", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteSchedule_BindsRoute()
    {
        var response = await Client.DeleteAsync("/api/Workschedule/9");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSchedule_BindsRouteAndBody()
    {
        var payload = new
        {
            StartTime = DateTime.UtcNow.AddDays(3),
            EndTime = DateTime.UtcNow.AddDays(6)
        };

        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/Workschedule/15")
        {
            Content = JsonContent(payload)
        };
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(15, WorkScheduleService.LastScheduleId);
        Assert.Equal(payload.StartTime, WorkScheduleService.LastStartDate);
        Assert.Equal(payload.EndTime, WorkScheduleService.LastEndDate);
    }

    [Fact]
    public async Task GetSchedule_BindsRoute()
    {
        var response = await Client.GetAsync("/api/Workschedule/21");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetActiveScheduleForDate_BindsQuery()
    {
        var date = DateTime.UtcNow.AddDays(1).ToString("O");
        var response = await Client.GetAsync($"/api/Workschedule/active?startDate={Uri.EscapeDataString(date)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSchedules_BindsQueryParameters()
    {
        var response = await Client.GetAsync(
            "/api/Workschedule/all?Page=2&PageSize=5&Status=Active&Searchstring=week");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SetScheduleInactive_BindsRoute()
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/Workschedule/33/inactive");
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SetScheduleActive_BindsRoute()
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/Workschedule/44/active");
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
