using System.Net;
using IntegrationTests.Infrastructure;
using Schichtpilot.Models.Enums;
using Xunit;

namespace IntegrationTests.Controllers;

public class AbsenceControllerTests : IntegrationTestBase
{
    public AbsenceControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateAbsence_BindsBodyAndRoutesToService()
    {
        var payload = new
        {
            StartDate = DateTime.UtcNow.Date.AddDays(2),
            EndDate = DateTime.UtcNow.Date.AddDays(4),
            AbsenceType = nameof(AbsenceTypeEnum.Vacation),
            Message = "Family event"
        };

        var response = await Client.PostAsync("/api/Absence", JsonContent(payload));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(1L, AbsenceService.LastCreateUserId);
        Assert.NotNull(AbsenceService.LastCreateDto);
        Assert.Equal(payload.StartDate, AbsenceService.LastCreateDto?.StartDate);
        Assert.Equal(payload.EndDate, AbsenceService.LastCreateDto?.EndDate);
        Assert.Equal(payload.AbsenceType, AbsenceService.LastCreateDto?.AbsenceType);
        Assert.Equal(payload.Message, AbsenceService.LastCreateDto?.Message);
    }

    [Fact]
    public async Task DeleteAbsence_BindsRouteId()
    {
        var response = await Client.DeleteAsync("/api/Absence/55");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(55, AbsenceService.LastDeleteAbsenceId);
        Assert.Equal(1L, AbsenceService.LastDeleteUserId);
    }

    [Fact]
    public async Task UpdateAbsence_BindsRouteAndBody()
    {
        var payload = new
        {
            Status = nameof(AbsenceStatusEnum.Approved),
            ManagerMessage = "Approved"
        };

        var response = await Client.PutAsync("/api/Absence/77", JsonContent(payload));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(77, AbsenceService.LastUpdateAbsenceId);
        Assert.NotNull(AbsenceService.LastUpdateDto);
        Assert.Equal(payload.Status, AbsenceService.LastUpdateDto?.Status);
        Assert.Equal(payload.ManagerMessage, AbsenceService.LastUpdateDto?.ManagerMessage);
    }

    [Fact]
    public async Task GetAbsence_BindsRouteId()
    {
        var response = await Client.GetAsync("/api/Absence/12");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(12, AbsenceService.LastGetAbsenceId);
    }

    [Fact]
    public async Task GetUserAbsences_BindsQueryParameters()
    {
        var response = await Client.GetAsync(
            "/api/Absence/user?Page=1&PageSize=10&Status=Approved&AbsenceType=Vacation&Searchstring=anna");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.NotNull(AbsenceService.LastUserAbsencesPagination);
        Assert.Equal(1, AbsenceService.LastUserAbsencesPagination?.Page);
        Assert.Equal(10, AbsenceService.LastUserAbsencesPagination?.PageSize);

        Assert.NotNull(AbsenceService.LastUserAbsencesFilter);
        Assert.Contains(AbsenceStatusEnum.Approved, AbsenceService.LastUserAbsencesFilter?.Status ?? new List<AbsenceStatusEnum>());
        Assert.Contains(AbsenceTypeEnum.Vacation, AbsenceService.LastUserAbsencesFilter?.AbsenceType ?? new List<AbsenceTypeEnum>());
        Assert.Equal("anna", AbsenceService.LastUserAbsencesFilter?.Searchstring);
        Assert.Equal(1L, AbsenceService.LastUserAbsencesUserId);
    }

    [Fact]
    public async Task GetAllAbsences_BindsQueryParameters()
    {
        var response = await Client.GetAsync(
            "/api/Absence/all?Page=2&PageSize=5&Status=Denied&AbsenceType=Vacation");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.NotNull(AbsenceService.LastAllAbsencesPagination);
        Assert.Equal(2, AbsenceService.LastAllAbsencesPagination?.Page);
        Assert.Equal(5, AbsenceService.LastAllAbsencesPagination?.PageSize);

        Assert.NotNull(AbsenceService.LastAllAbsencesFilter);
        Assert.Contains(AbsenceStatusEnum.Denied, AbsenceService.LastAllAbsencesFilter?.Status ?? new List<AbsenceStatusEnum>());
        Assert.Contains(AbsenceTypeEnum.Vacation, AbsenceService.LastAllAbsencesFilter?.AbsenceType ?? new List<AbsenceTypeEnum>());
    }
}
