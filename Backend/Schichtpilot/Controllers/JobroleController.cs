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

    [HttpPost]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CreateJobRoleAsync([FromBody, Required] CreateJobRoleDto request)
    {
        await this._jobRoleService.CreateJobRoleAsync(request);
        return Created();
    }
    
    [HttpPut("{jobRoleId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateJobRoleAsync([FromRoute, Required] int jobRoleId, [FromBody, Required] EditJobRoleDto request)
    {
        await this._jobRoleService.UpdateJobRoleAsync(jobRoleId, request);
        return NoContent();
    }
    
    [HttpPost("{jobRoleId}/dependency/{dependencyId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddJobRoleDependencyAsync([FromRoute, Required] int jobRoleId, [FromRoute, Required] int dependencyId)
    {
        await this._jobRoleService.AddDependenciesToJobRoleAsync(jobRoleId, dependencyId);
        return NoContent();
    }
    
    [HttpDelete("{jobRoleId}/dependency/{dependencyId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteJobRoleDependencyAsync([FromRoute, Required] int jobRoleId, [FromRoute, Required] int dependencyId)
    {
        await this._jobRoleService.RemoveDependenciesToJobRoleAsync(jobRoleId, dependencyId);
        return NoContent();
    }
    
    [HttpPost("{jobRoleId}/user/{userId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddUsersToRoleAsync([FromRoute, Required] int jobRoleId, [FromQuery, Required] long userId)
    {
        await this._jobRoleService.AddUserToJobRoleAsync(jobRoleId, userId);
        return NoContent();
    }
    
    [HttpDelete("{jobRoleId}/user/{userId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveUsersFromRoleAsync([FromRoute, Required] int jobRoleId, [FromQuery, Required] long userId)
    {
        await this._jobRoleService.RemoveUserFromJobRoleAsync(jobRoleId, userId);
        return NoContent();
    }
    
    [HttpDelete("{jobRoleId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteJobRoleAsync([FromRoute, Required] int jobRoleId)
    {
        await this._jobRoleService.DeleteRoleAsync(jobRoleId);
        return NoContent();
    }
    
    [HttpGet("{jobRoleId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType( typeof(JobRoleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobRoleAsync([FromRoute, Required] int jobRoleId)
    {
        return Ok(await this._jobRoleService.GetJobRoleAsync(jobRoleId));
    }
    
    [HttpGet("all")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType( typeof(QueryableJobRoleResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobRolesAsync([FromQuery] GetJobRolesRequest request)
    {
        var paginationDto = this._mapper.Map<PaginationDto>(request);
        return Ok(await this._jobRoleService.GetJobRolesAsync(paginationDto, request.Searchstring));
    }
}