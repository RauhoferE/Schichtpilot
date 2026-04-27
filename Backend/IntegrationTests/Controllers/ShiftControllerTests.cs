using System.Net;
using IntegrationTests.Infrastructure;
using Xunit;

namespace IntegrationTests.Controllers;

public class ShiftControllerTests : IntegrationTestBase
{
    public ShiftControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateShift_BindsBodyAndRoutes()
    {
        var payload = new
        {
            Name = "Morning Shift",
            ColorAsHex = "#FFAA00",
            TimeSlots = new[]
            {
                new
                {
                    DayOfWeek = 0,
                    StartTime = "08:00:00",
                    EndTime = "16:00:00",
                    Breaks = Array.Empty<object>()
                }
            },
            JobRequirements = new[]
            {
                new
                {
                    JobId = 1,
                    Name = "Cook",
                    RequiredStaffCount = 2
                }
            }
        };

        var response = await Client.PostAsync("/api/Shift", JsonContent(payload));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task UpdateShift_BindsRouteAndBody()
    {
        var payload = new
        {
            Name = "Updated Shift",
            ColorAsHex = "#00AAFF"
        };

        var response = await Client.PutAsync("/api/Shift/10", JsonContent(payload));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteShift_BindsRoute()
    {
        var response = await Client.DeleteAsync("/api/Shift/11");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTimeslot_BindsRoute()
    {
        var response = await Client.DeleteAsync("/api/Shift/12/timeslot/5");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AddTimeslot_BindsRouteAndBody()
    {
        var payload = new
        {
            DayOfWeek = 1,
            StartTime = "09:00:00",
            EndTime = "17:00:00",
            Breaks = Array.Empty<object>()
        };

        var response = await Client.PostAsync("/api/Shift/13/timeslot", JsonContent(payload));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTimeslot_BindsRouteAndBody()
    {
        var payload = new
        {
            DayOfWeek = 2,
            StartTime = "10:00:00",
            EndTime = "18:00:00",
            Breaks = Array.Empty<object>()
        };

        var response = await Client.PutAsync("/api/Shift/14/timeslot/7", JsonContent(payload));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AddJobRequirement_BindsRouteAndBody()
    {
        var payload = new
        {
            JobId = 3,
            Name = "Waiter",
            RequiredStaffCount = 4
        };

        var response = await Client.PostAsync("/api/Shift/15/job", JsonContent(payload));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ChangeJobRequirementCount_BindsRouteAndQuery()
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/Shift/20/job/9?staffCount=5");
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(20, ShiftService.LastChangeRequiredStaffShiftId);
        Assert.Equal(9, ShiftService.LastChangeRequiredStaffJobId);
        Assert.Equal(5, ShiftService.LastChangeRequiredStaffCount);
    }

    [Fact]
    public async Task RemoveJobRequirement_BindsRoute()
    {
        var response = await Client.DeleteAsync("/api/Shift/21/job/4");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
