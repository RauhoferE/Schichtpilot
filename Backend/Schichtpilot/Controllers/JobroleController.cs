using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;
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

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CreateJobRoleAsync([FromBody, Required] CreateJobRoleDto request)
    {
        await this._jobRoleService.CreateJobRoleAsync(request);
        return NoContent();
    }
    
    [HttpPut("{jobRoleId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateJobRoleAsync([FromRoute, Required] int jobRoleId, [FromBody, Required] EditJobRoleDto request)
    {
        await this._jobRoleService.UpdateJobRoleAsync(jobRoleId, request);
        return NoContent();
    }
    
    [HttpPost("{jobRoleId}/dependency/{dependencyId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddJobRoleDependencyAsync([FromRoute, Required] int jobRoleId, [FromRoute, Required] int dependencyId)
    {
        await this._jobRoleService.AddDependenciesToJobRoleAsync(jobRoleId, dependencyId);
        return NoContent();
    }
    
    [HttpDelete("{jobRoleId}/dependency/{dependencyId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteJobRoleDependencyAsync([FromRoute, Required] int jobRoleId, [FromRoute, Required] int dependencyId)
    {
        await this._jobRoleService.RemoveDependenciesToJobRoleAsync(jobRoleId, dependencyId);
        return NoContent();
    }
    
    [HttpPost("{jobRoleId}/user/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddUsersToRoleAsync([FromRoute, Required] int jobRoleId, [FromBody, Required] long[] userIds)
    {
        await this._jobRoleService.AddUsersToJobRoleAsync(jobRoleId, userIds.ToList());
        return NoContent();
    }
    
    [HttpDelete("{jobRoleId}/user/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveUsersFromRoleAsync([FromRoute, Required] int jobRoleId, [FromBody, Required] long[] userIds)
    {
        await this._jobRoleService.AddUsersToJobRoleAsync(jobRoleId, userIds.ToList());
        return NoContent();
    }
}