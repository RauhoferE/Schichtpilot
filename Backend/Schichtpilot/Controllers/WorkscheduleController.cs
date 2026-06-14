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

/// <summary>
/// Provides an endpoint to generate, delete and update workschedule.
/// </summary>
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


    /// <summary>
    /// Generates a new workschedule with the given parameters.
    /// </summary>
    /// <param name="dto"> The parameters for the new workschedule. </param>
    /// <returns> Returns a created status code response. </returns>
    [HttpPost("generate")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> GenerateScheduleAsync([FromBody, Required] GenerateScheduleDto dto)
    {
        await this._workScheduleService.GenerateScheduleAsync(dto);
        return Created();
    }

    /// <summary>
    /// Regenerates an existing workschedule.
    /// </summary>
    /// <param name="scheduleId"> The workschedule to be regenerated. </param>
    /// <returns> Return a no content status code response. </returns>
    [HttpPost("generate/{scheduleId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GenerateScheduleAsync([FromRoute, Required] int scheduleId)
    {
        await this._workScheduleService.ReGenerateScheduleAsync(scheduleId);
        return NoContent();
    }

    /// <summary>
    /// Publishes an existing workschedule.
    /// </summary>
    /// <param name="scheduleId"> The workschedule to be published. </param>
    /// <returns> Returns a no content status code response. </returns>
    [HttpPost("publish/{scheduleId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PublishScheduleAsync([FromRoute, Required] int scheduleId)
    {
        await this._workScheduleService.PublishScheduleAsync(scheduleId);
        return NoContent();
    }

    /// <summary>
    /// Deletes an existing workschedule.
    /// </summary>
    /// <param name="scheduleId"> The workschedule to be deleted. </param>
    /// <returns> Returns a no content status code response. </returns>
    [HttpDelete("{scheduleId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteScheduleAsync([FromRoute, Required] int scheduleId)
    {
        await this._workScheduleService.DeleteScheduleAsync(scheduleId);
        return NoContent();
    }

    /// <summary>
    /// Updates the name and date of the workschedule.
    /// </summary>
    /// <param name="scheduleId"> The workschedule to be updated. </param>
    /// <param name="dto"> The new parameters of the schedule. </param>
    /// <returns> Returns a no content status code response. </returns>
    [HttpPatch("{scheduleId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateScheduleAsync([FromRoute, Required] int scheduleId, [FromBody, Required] UpdateScheduleRequest dto)
    {
        await this._workScheduleService.ChangeScheduleDateAsync(scheduleId, dto.StartTime, dto.EndTime);
        return NoContent();
    }

    /// <summary>
    /// Gets a specific schedule.
    /// </summary>
    /// <param name="scheduleId"> The schedule to be requested. </param>
    /// <returns> Returns the schedule as <see cref="WorkScheduleDto"/>. </returns>
    [HttpGet("{scheduleId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(typeof(WorkScheduleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScheduleAsync([FromRoute, Required] int scheduleId)
    {
        return Ok(await this._workScheduleService.GetScheduleAsync(scheduleId));
    }

    /// <summary>
    /// Gets an existing workschedule by startdate.
    /// </summary>
    /// <param name="startDate"> The startdate of the workschedule. </param>
    /// <returns> Returns the schedule as <see cref="WorkScheduleDto"/>. </returns>
    [HttpGet("active")]
    [Authorize(Roles = $"{UserRolesClass.Admin},{UserRolesClass.User}")]
    [ProducesResponseType(typeof(WorkScheduleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveScheduleForDateAsync([FromQuery, Required] DateTime startDate)
    {
        return Ok(await this._workScheduleService.GetActiveScheduleForDateAsync(startDate));
    }

    /// <summary>
    /// Gets all available schedules.
    /// </summary>
    /// <param name="request"> The schedule sorting and filtering request. </param>
    /// <returns> Returns the schedules as <see cref="QueryableSchedules"/>. </returns>
    [HttpGet("all")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(typeof(QueryableSchedules), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchedulesAsync([FromQuery, Required] GetSchedulesRequest request)
    {
        var paginationDto = this._mapper.Map<GetSchedulesRequest, PaginationDto>(request);
        var filterDto = this._mapper.Map<GetSchedulesRequest, ScheduleFilterDot>(request);
        return Ok(await this._workScheduleService.GetSchedulesAsync(paginationDto, filterDto));
    }

    /// <summary>
    /// Sets a schedule as inactive.
    /// </summary>
    /// <param name="scheduleId"> The schedule to be set as inactive. </param>
    /// <returns> Returns a no content status code response. </returns>
    [HttpPatch("{scheduleId}/inactive")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetScheduleInActiveAsync([FromRoute, Required] int scheduleId)
    {
        await this._workScheduleService.SetScheduleOfflineAsync(scheduleId);
        return NoContent();
    }

    /// <summary>
    /// Sets a schedule as active.
    /// </summary>
    /// <param name="scheduleId">The schedule to be set as active.</param>
    /// <returns> Returns a no content status code response. </returns>
    [HttpPatch("{scheduleId}/active")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetScheduleAsActiveAsync([FromRoute, Required] int scheduleId)
    {
        await this._workScheduleService.SetScheduleActiveAsync(scheduleId);
        return NoContent();
    }
}