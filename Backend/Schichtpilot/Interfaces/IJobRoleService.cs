using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Interfaces;

/// <summary>
/// Orchestrates job role related operations including creating, updating, deleting job roles.
/// Also includes adding, removing users from jobs and managing job dependencies on other jobs.
/// </summary>
public interface IJobRoleService
{
    /// <summary>
    /// Creates a new job role.
    /// </summary>
    /// <param name="jobRole"> The new job role to be added. </param>
    /// <returns></returns>
    Task CreateJobRoleAsync(CreateJobRoleDto jobRole);

    /// <summary>
    /// Updates an existing job role.
    /// </summary>
    /// <param name="id"> The job role to be updated. </param>
    /// <param name="jobRole"> The updated parameters of a job. </param>
    /// <returns></returns>
    Task UpdateJobRoleAsync(int id, EditJobRoleDto jobRole);

    /// <summary>
    /// Adds a new job dependency to an existing job. 
    /// </summary>
    /// <param name="jobRoleId"> The job role to be updated. </param>
    /// <param name="dependencyId"> The id of the job role to be added as a dependency. </param>
    /// <returns></returns>
    Task AddDependenciesToJobRoleAsync(int jobRoleId, int dependencyId);

    /// <summary>
    /// Removes an existing job role as a dependency from a job role.
    /// </summary>
    /// <param name="jobRoleId"> The job role that contains the dependency. </param>
    /// <param name="dependencyId"> The dependency to be removed. </param>
    /// <returns></returns>
    Task RemoveDependenciesToJobRoleAsync(int jobRoleId, int dependencyId);

    /// <summary>
    /// Adds an existing user to a job role.
    /// </summary>
    /// <param name="id"> The job role to be added to the user. </param>
    /// <param name="userId"> The user that gains the specific role. </param>
    /// <returns></returns>
    Task AddUserToJobRoleAsync(int id, long userId);

    /// <summary>
    /// Removes a user from a job role. 
    /// </summary>
    /// <param name="id"> The job role to be removed from the user. </param>
    /// <param name="userId"> The user that losses the specific role. </param>
    /// <returns></returns>
    Task RemoveUserFromJobRoleAsync(int id, long userId);

    /// <summary>
    /// Deletes an existing role.
    /// </summary>
    /// <param name="id"> The role to be deleted. </param>
    /// <returns></returns>
    Task DeleteRoleAsync(int id);

    /// <summary>
    /// Gets the details of a job role. 
    /// </summary>
    /// <param name="id"> The targeted jobrole. </param>
    /// <returns> Returns the job role as <see cref="JobRoleDto"/>. </returns>
    Task<JobRoleDto> GetJobRoleAsync(int id);

    /// <summary>
    /// Gets a list of job roles.
    /// </summary>
    /// <param name="paginationDto"> The pagination element. </param>
    /// <param name="searchString"> The job role to look for. </param>
    /// <returns> Returns the job roles as <see cref="QueryableJobRoleResponse"/>. </returns>
    Task<QueryableJobRoleResponse> GetJobRolesAsync(PaginationDto paginationDto, string? searchString);
}