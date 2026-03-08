using Data.Entities;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Interfaces;

public interface IJobRoleService
{
    Task CreateJobRoleAsync(CreateJobRoleDto jobRole);
    
    Task UpdateJobRoleAsync(int id, EditJobRoleDto jobRole);
    
    Task AddDependenciesToJobRoleAsync(int jobRoleId, int dependencyId);
    
    Task RemoveDependenciesToJobRoleAsync(int jobRoleId, int dependencyId);
    
    Task AddUsersToJobRoleAsync(int id, List<long> userIds);
    
    Task RemoveUsersFromJobRoleAsync(int id, List<long> userIds);
    
    Task<JobRoleDto> GetJobRoleAsync(int id);
    
    Task<QueryableJobRoleResponse> GetJobRolesAsync(PaginationDto paginationDto, string? searchString);
}