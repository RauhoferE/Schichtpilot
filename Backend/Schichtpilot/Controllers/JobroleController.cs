using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Requests;
using Schichtpilot.Models.Responses;

namespace Schichtpilot.Controllers;

/// <summary>
/// Provides an endpoint to get, add, delete and update jobroles and their dependencies.
/// Also used to add and remove jobroles to and from existing users.
/// </summary>
[Controller]
[Route("api/[controller]")]
public class JobroleController : Controller
{
    public JobroleController(IJobRoleService jobRoleService, IMapper mapper)
    {
        _jobRoleService = jobRoleService ?? throw new ArgumentNullException(nameof(jobRoleService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    private readonly IJobRoleService _jobRoleService;
    private readonly IMapper _mapper;

    /// <summary>
    /// Creates a new jobrole.
    /// </summary>
    /// <param name="request"> The jobrole to be created. </param>
    /// <returns> Returns a created status code. </returns>
    [HttpPost]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CreateJobRoleAsync([FromBody, Required] CreateJobRoleDto request)
    {
        await this._jobRoleService.CreateJobRoleAsync(request);
        return Created();
    }

    /// <summary>
    /// Updates a jobrole.
    /// </summary>
    /// <param name="jobRoleId"> The job role to be updated. </param>
    /// <param name="request"> The new name and description for the jobrole. </param>
    /// <returns> Returns a no content result. </returns>
    [HttpPut("{jobRoleId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateJobRoleAsync([FromRoute, Required] int jobRoleId, [FromBody, Required] EditJobRoleDto request)
    {
        await this._jobRoleService.UpdateJobRoleAsync(jobRoleId, request);
        return NoContent();
    }

    /// <summary>
    /// Adds a new job role dependecy to an existing job.
    /// </summary>
    /// <param name="jobRoleId"> The job role that needs a dependency. </param>
    /// <param name="dependencyId"> The job that will be added as a dependency. </param>
    /// <returns> Returns a no content result. </returns>
    [HttpPost("{jobRoleId}/dependency/{dependencyId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddJobRoleDependencyAsync([FromRoute, Required] int jobRoleId, [FromRoute, Required] int dependencyId)
    {
        await this._jobRoleService.AddDependenciesToJobRoleAsync(jobRoleId, dependencyId);
        return NoContent();
    }

    /// <summary>
    /// Deletes a jobrole dependency.
    /// </summary>
    /// <param name="jobRoleId"> The jobrole that will lose its dependency. </param>
    /// <param name="dependencyId"> The jobrole dependency to be removed. </param>
    /// <returns> Returns a no content response. </returns>
    [HttpDelete("{jobRoleId}/dependency/{dependencyId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteJobRoleDependencyAsync([FromRoute, Required] int jobRoleId, [FromRoute, Required] int dependencyId)
    {
        await this._jobRoleService.RemoveDependenciesToJobRoleAsync(jobRoleId, dependencyId);
        return NoContent();
    }

    /// <summary>
    /// Adds an existing user to an existing role.
    /// </summary>
    /// <param name="jobRoleId"> The jobrole to be added. </param>
    /// <param name="userId"> The user that gets the jobrole. </param>
    /// <returns> Returns a no content response. </returns>
    [HttpPost("{jobRoleId}/user/{userId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddUsersToRoleAsync([FromRoute, Required] int jobRoleId, [FromRoute, Required] long userId)
    {
        await this._jobRoleService.AddUserToJobRoleAsync(jobRoleId, userId);
        return NoContent();
    }

    /// <summary>
    /// Removes a jobrole from a user.
    /// </summary>
    /// <param name="jobRoleId"> The jobrole to be removed. </param>
    /// <param name="userId"> The user that will lose the jobrole. </param>
    /// <returns> Returns a no content response. </returns>
    [HttpDelete("{jobRoleId}/user/{userId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveUsersFromRoleAsync([FromRoute, Required] int jobRoleId, [FromRoute, Required] long userId)
    {
        await this._jobRoleService.RemoveUserFromJobRoleAsync(jobRoleId, userId);
        return NoContent();
    }

    /// <summary>
    /// Deletes an existing jobrole.
    /// </summary>
    /// <param name="jobRoleId"> The jobrole to be removed. </param>
    /// <returns> Returns a no content response. </returns>
    [HttpDelete("{jobRoleId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteJobRoleAsync([FromRoute, Required] int jobRoleId)
    {
        await this._jobRoleService.DeleteRoleAsync(jobRoleId);
        return NoContent();
    }

    /// <summary>
    /// Gets the details of a jobrole.
    /// </summary>
    /// <param name="jobRoleId"> The jobrole to get. </param>
    /// <returns> Returns the jobrole as <see cref="JobRoleDto"/>. </returns>
    [HttpGet("{jobRoleId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(typeof(JobRoleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobRoleAsync([FromRoute, Required] int jobRoleId)
    {
        return Ok(await this._jobRoleService.GetJobRoleAsync(jobRoleId));
    }

    /// <summary>
    /// Gets all available jobroles.
    /// </summary>
    /// <param name="request"> The jobrole sorting and filtering request. </param>
    /// <returns> Returns jobroles as <see cref="QueryableJobRoleResponse"/>. </returns>
    [HttpGet("all")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(typeof(QueryableJobRoleResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobRolesAsync([FromQuery] GetJobRolesRequest request)
    {
        var paginationDto = this._mapper.Map<PaginationDto>(request);
        return Ok(await this._jobRoleService.GetJobRolesAsync(paginationDto, request.Searchstring));
    }
}