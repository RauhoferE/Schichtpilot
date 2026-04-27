using System.Net;
using IntegrationTests.Infrastructure;
using Xunit;

namespace IntegrationTests.Controllers;

public class JobroleControllerTests : IntegrationTestBase
{
    public JobroleControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateJobRole_BindsBodyAndRoutes()
    {
        var payload = new
        {
            Name = "Chef",
            Description = "Leads the kitchen team",
            DependentOnJobRoleIds = new[] { 2, 3 }
        };

        var response = await Client.PostAsync("/api/Jobrole", JsonContent(payload));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(JobRoleService.LastCreatedJobRole);
        Assert.Equal(payload.Name, JobRoleService.LastCreatedJobRole?.Name);
        Assert.Equal(payload.Description, JobRoleService.LastCreatedJobRole?.Description);
        Assert.Equal(payload.DependentOnJobRoleIds.Length, JobRoleService.LastCreatedJobRole?.DependentOnJobRoleIds.Count);
    }

    [Fact]
    public async Task UpdateJobRole_BindsRouteAndBody()
    {
        var payload = new
        {
            Name = "Sous Chef",
            Description = "Assists the head chef"
        };

        var response = await Client.PutAsync("/api/Jobrole/15", JsonContent(payload));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(15, JobRoleService.LastUpdatedJobRoleId);
        Assert.NotNull(JobRoleService.LastUpdatedJobRole);
        Assert.Equal(payload.Name, JobRoleService.LastUpdatedJobRole?.Name);
        Assert.Equal(payload.Description, JobRoleService.LastUpdatedJobRole?.Description);
    }

    [Fact]
    public async Task AddDependency_BindsRoute()
    {
        var response = await Client.PostAsync("/api/Jobrole/8/dependency/4", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal((8, 4), JobRoleService.LastAddedDependency);
    }

    [Fact]
    public async Task RemoveDependency_BindsRoute()
    {
        var response = await Client.DeleteAsync("/api/Jobrole/9/dependency/5");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal((9, 5), JobRoleService.LastRemovedDependency);
    }

    [Fact]
    public async Task AddUser_BindsRoute()
    {
        var response = await Client.PostAsync("/api/Jobrole/3/user/42", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal((3, 42L), JobRoleService.LastAddedUser);
    }

    [Fact]
    public async Task RemoveUser_BindsRoute()
    {
        var response = await Client.DeleteAsync("/api/Jobrole/3/user/42");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal((3, 42L), JobRoleService.LastRemovedUser);
    }

    [Fact]
    public async Task DeleteJobRole_BindsRoute()
    {
        var response = await Client.DeleteAsync("/api/Jobrole/22");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(22, JobRoleService.LastDeletedJobRoleId);
    }

    [Fact]
    public async Task GetJobRole_BindsRoute()
    {
        var response = await Client.GetAsync("/api/Jobrole/33");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(33, JobRoleService.LastFetchedJobRoleId);
    }

    [Fact]
    public async Task GetJobRoles_BindsQueryParameters()
    {
        var response = await Client.GetAsync("/api/Jobrole/all?Page=2&PageSize=20&Searchstring=chef");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(JobRoleService.LastPagination);
        Assert.Equal(2, JobRoleService.LastPagination?.Page);
        Assert.Equal(20, JobRoleService.LastPagination?.PageSize);
        Assert.Equal("chef", JobRoleService.LastSearchString);
    }
}
