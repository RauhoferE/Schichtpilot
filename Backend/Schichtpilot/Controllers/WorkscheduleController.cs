using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Enums;
using Schichtpilot.Models.Requests;

namespace Schichtpilot.Controllers;

[Controller]
[Route("api/[controller]")]
public class WorkscheduleController : Controller
{
    public WorkscheduleController(IMapper mapper, IWorkScheduleService workScheduleService)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _workScheduleService = workScheduleService ?? throw new ArgumentNullException(nameof(workScheduleService));
    }

    private readonly IMapper _mapper;

    private readonly IWorkScheduleService _workScheduleService;


    [HttpPost("generate")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> GenerateScheduleAsync([FromBody, Required] GenerateScheduleDto dto)
    {
        await this._workScheduleService.GenerateScheduleAsync(dto);
        return Created();
    }

    [HttpPost("generate/{scheduleId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GenerateScheduleAsync([FromRoute, Required] int scheduleId)
    {
        await this._workScheduleService.ReGenerateScheduleAsync(scheduleId);
        return NoContent();
    }

    [HttpPost("publish/{scheduleId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PublishScheduleAsync([FromRoute, Required] int scheduleId)
    {
        await this._workScheduleService.PublishScheduleAsync(scheduleId);
        return NoContent();
    }

    [HttpDelete("{scheduleId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteScheduleAsync([FromRoute, Required] int scheduleId)
    {
        await this._workScheduleService.DeleteScheduleAsync(scheduleId);
        return NoContent();
    }

    [HttpPatch("{scheduleId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateScheduleAsync([FromRoute, Required] int scheduleId, [FromBody, Required] UpdateScheduleRequest dto)
    {
        await this._workScheduleService.ChangeScheduleDateAsync(scheduleId, dto.StartTime, dto.EndTime);
        return NoContent();
    }

    [HttpGet("{scheduleId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(typeof(WorkScheduleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScheduleAsync([FromRoute, Required] int scheduleId)
    {
        return Ok(await this._workScheduleService.GetScheduleAsync(scheduleId));
    }

    [HttpGet("active")]
    [Authorize(Roles = UserRolesClass.User)]
    [ProducesResponseType(typeof(WorkScheduleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveScheduleForDateAsync([FromQuery, Required] DateTime startDate)
    {
        return Ok(await this._workScheduleService.GetActiveScheduleForDateAsync(startDate));
    }

    [HttpGet("all")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(typeof(QueryableSchedules), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchedulesAsync([FromBody, Required] GetSchedulesRequest request)
    {
        var paginationDto = this._mapper.Map<GetSchedulesRequest, PaginationDto>(request);
        var filterDto = this._mapper.Map<GetSchedulesRequest, ScheduleFilterDot>(request);
        return Ok(await this._workScheduleService.GetSchedulesAsync(paginationDto, filterDto));
    }

    [HttpPatch("{scheduleId}/inactive")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetScheduleInActiveAsync([FromRoute, Required] int scheduleId)
    {
        await this._workScheduleService.SetScheduleOfflineAsync(scheduleId);
        return NoContent();
    }

    [HttpPatch("{scheduleId}/active")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetScheduleAsActiveAsync([FromRoute, Required] int scheduleId)
    {
        await this._workScheduleService.SetScheduleActiveAsync(scheduleId);
        return NoContent();
    }
}