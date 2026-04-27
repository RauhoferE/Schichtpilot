
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;

namespace IntegrationTests.Fakes;

public class FakeJobRoleService : IJobRoleService
{
    public int? LastCreatedJobRoleId { get; private set; }
    public CreateJobRoleDto? LastCreatedJobRole { get; private set; }

    public int? LastUpdatedJobRoleId { get; private set; }
    public EditJobRoleDto? LastUpdatedJobRole { get; private set; }

    public (int JobRoleId, int DependencyId)? LastAddedDependency { get; private set; }
    public (int JobRoleId, int DependencyId)? LastRemovedDependency { get; private set; }

    public (int JobRoleId, long UserId)? LastAddedUser { get; private set; }
    public (int JobRoleId, long UserId)? LastRemovedUser { get; private set; }

    public int? LastDeletedJobRoleId { get; private set; }
    public int? LastFetchedJobRoleId { get; private set; }

    public PaginationDto? LastPagination { get; private set; }
    public string? LastSearchString { get; private set; }

    public Task CreateJobRoleAsync(CreateJobRoleDto jobRole)
    {
        LastCreatedJobRole = jobRole;
        return Task.CompletedTask;
    }

    public Task UpdateJobRoleAsync(int id, EditJobRoleDto jobRole)
    {
        LastUpdatedJobRoleId = id;
        LastUpdatedJobRole = jobRole;
        return Task.CompletedTask;
    }

    public Task AddDependenciesToJobRoleAsync(int jobRoleId, int dependencyId)
    {
        LastAddedDependency = (jobRoleId, dependencyId);
        return Task.CompletedTask;
    }

    public Task RemoveDependenciesToJobRoleAsync(int jobRoleId, int dependencyId)
    {
        LastRemovedDependency = (jobRoleId, dependencyId);
        return Task.CompletedTask;
    }

    public Task AddUserToJobRoleAsync(int id, long userId)
    {
        LastAddedUser = (id, userId);
        return Task.CompletedTask;
    }

    public Task RemoveUserFromJobRoleAsync(int id, long userId)
    {
        LastRemovedUser = (id, userId);
        return Task.CompletedTask;
    }

    public Task DeleteRoleAsync(int id)
    {
        LastDeletedJobRoleId = id;
        return Task.CompletedTask;
    }

    public Task<JobRoleDto> GetJobRoleAsync(int id)
    {
        LastFetchedJobRoleId = id;
        return Task.FromResult(new JobRoleDto
        {
            Id = id,
            Name = "Fake Role",
            Description = "Fake Description",
            Users = new List<UserDto>(),
            DependentOn = new List<JobRoleDto>(),
            Prerequisites = new List<JobRoleDto>()
        });
    }

    public Task<QueryableJobRoleResponse> GetJobRolesAsync(PaginationDto paginationDto, string? searchString)
    {
        LastPagination = paginationDto;
        LastSearchString = searchString;

        return Task.FromResult(new QueryableJobRoleResponse
        {
            Count = 0,
            JobRoles = Array.Empty<JobRoleShortDto>()
        });
    }
}
