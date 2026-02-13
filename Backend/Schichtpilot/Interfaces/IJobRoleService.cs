using Data.Entities;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Interfaces;

public interface IJobRoleService
{
    Task CreateJobRoleAsync(CreateJobRoleDto jobRole);
    
    Task UpdateJobRoleAsync(int id, EditJobRoleDto jobRole);
    
    Task AddDependenciesToJobRole(int jobRoleId, int dependencyId);
    
    Task RemoveDependenciesToJobRole(int jobRoleId, int dependencyId);
    
    Task AddUsersToJobRoleAsync(int id, List<int> userIds);
    
    Task RemoveUsersFromJobRoleAsync(int id, List<int> userIds);
    
    Task<JobRoleDto> GetJobRoleAsync(int id);
    
    Task<QueryableJobRoleResponse> GetJobRolesAsync(PaginationDto paginationDto);
}