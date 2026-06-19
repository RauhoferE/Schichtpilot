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
/// Provides an endpoint to create, modify and get shifts.
/// </summary>
[Controller]
[Route("api/[controller]")]
public class ShiftController : Controller
{
    public ShiftController(IMapper mapper, IShiftService shiftService)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _shiftService = shiftService ?? throw new ArgumentNullException(nameof(shiftService));
    }

    private readonly IMapper _mapper;

    private readonly IShiftService _shiftService;

    /// <summary>
    /// Creates a new shift.
    /// </summary>
    /// <param name="request"> The shift to be created. </param>
    /// <returns> Returns a created status code response. </returns>
    [HttpPost]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateShiftAsync([FromBody, Required] CreateShiftDto request)
    {
        await _shiftService.CreateShiftAsync(request);
        return Created();
    }

    /// <summary>
    /// Gets the shift with the specified id.
    /// </summary>
    /// <param name="shiftId"> The shift to get. </param>
    /// <returns> Returns the shift as a <see cref="ShiftDto"/>. </returns>
    [HttpGet("{shiftId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(typeof(ShiftDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShiftAsync([FromRoute, Required] int shiftId)
    {
        return Ok(await _shiftService.GetShiftAsync(shiftId));
    }

    /// <summary>
    /// Gets all available shifts.
    /// </summary>
    /// <param name="request"> The shift sorting and filtering request. </param>
    /// <returns> Returns the shifts as <see cref="QueryableShiftResponse"/>. </returns>
    [HttpGet("all")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(typeof(QueryableShiftResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShiftsAsync([FromQuery, Required] GetShiftsRequest request)
    {
        var paginationDto = this._mapper.Map<GetShiftsRequest, PaginationDto>(request);
        var shiftFilterDto = this._mapper.Map<GetShiftsRequest, ShiftFilterDto>(request);
        return Ok(await _shiftService.GetShiftsAsync(paginationDto, shiftFilterDto));
    }

    /// <summary>
    /// Updates an existing shift.
    /// </summary>
    /// <param name="shiftId"> The shift to be updated. </param>
    /// <param name="request"> The new details of the shift. </param>
    /// <returns> Returns a no content status code response. </returns>
    [HttpPut("{shiftId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateShiftAsync([FromRoute, Required] int shiftId, [FromBody, Required] EditShiftDto request)
    {
        await _shiftService.ManageShiftAsync(shiftId, request);
        return NoContent();
    }

    /// <summary>
    /// Deletes an existing shift.
    /// </summary>
    /// <param name="shiftId"> The shift to be deleted. </param>
    /// <returns> Returns a no content status code reponse. </returns>
    [HttpDelete("{shiftId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteShiftAsync([FromRoute, Required] int shiftId)
    {
        await _shiftService.DeleteShiftAsync(shiftId);
        return NoContent();
    }

    /// <summary>
    /// Deletes a timeslot used by a shift.
    /// </summary>
    /// <param name="shiftId"> The shift that contains the timeslot. </param>
    /// <param name="timeSlotId"> The timeslot to be deleted. </param>
    /// <returns> Returns a no content status code response. </returns>
    [HttpDelete("{shiftId}/timeslot/{timeSlotId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTimeslotAsync([FromRoute, Required] int shiftId, [FromRoute, Required] int timeSlotId)
    {
        await _shiftService.DeleteTimeSlotAsync(shiftId, timeSlotId);
        return NoContent();
    }

    /// <summary>
    /// Adds a new timeslot to an existing shift.
    /// </summary>
    /// <param name="shiftId"> The shift to get a new timeslot. </param>
    /// <param name="timeSlotDto"> The timeslot to be added. </param>
    /// <returns> Returns a no content status code response. </returns>
    [HttpPost("{shiftId}/timeslot")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddTimeslotAsync([FromRoute, Required] int shiftId, [FromBody, Required] TimeSlotDto timeSlotDto)
    {
        await _shiftService.AddTimeSlotAsync(shiftId, timeSlotDto);
        return NoContent();
    }

    /// <summary>
    /// Updates an existing timeslot.
    /// </summary>
    /// <param name="shiftId"> The shift id with the timeslot. </param>
    /// <param name="timeSlotDto"> The updated timeslot parameters. </param>
    /// <param name="timeSlotId"> The timeslot to be updated. </param>
    /// <returns> Returns a no content status code response. </returns>
    [HttpPut("{shiftId}/timeslot/{timeSlotId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateTimeslotAsync([FromRoute, Required] int shiftId, [FromBody, Required] TimeSlotDto timeSlotDto, [FromRoute, Required] int timeSlotId)
    {
        timeSlotDto.Id = timeSlotId;
        await _shiftService.EditTimeSlotAsync(shiftId, timeSlotDto);
        return NoContent();
    }

    /// <summary>
    /// Adds a new job requirement to a shift.
    /// </summary>
    /// <param name="shiftId"> The shift that gets a new job requirement. </param>
    /// <param name="jobrequirementRequest"> The job to be added to the shift. </param>
    /// <returns> Returns a no content status code response. </returns>
    [HttpPost("{shiftId}/job")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddJobRequirementAsync([FromRoute, Required] int shiftId, [FromBody, Required] ShiftRequirementDto jobrequirementRequest)
    {
        await _shiftService.AddJobRequirementAsync(shiftId, jobrequirementRequest);
        return NoContent();
    }

    /// <summary>
    /// Changes the required number of people for a specific job needed for a shift.
    /// </summary>
    /// <param name="shiftId"> The shift to be modified. </param>
    /// <param name="jobId"> The job to be modified. </param>
    /// <param name="staffCount"> The new required number of people for a job. </param>
    /// <returns> Returns a no content status code response. </returns>
    /// <exception cref="Exception"> Thrown when not enough staff is put in as a requirement. </exception>
    [HttpPatch("{shiftId}/job/{jobId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangeJobRequirementCountAsync([FromRoute, Required] int shiftId, [FromRoute, Required] int jobId, [FromQuery, Required] int staffCount)
    {
        if (staffCount < 1)
        {
            throw new Exception("StaffCount must be greater than zero");
        }

        await _shiftService.ChangeRequiredStaffAsync(shiftId, jobId, staffCount);
        return NoContent();
    }

    /// <summary>
    /// Removes a job requirement from a shift.
    /// </summary>
    /// <param name="shiftId"> The shift to be modified. </param>
    /// <param name="jobId"> The job to be removed. </param>
    /// <returns> Returns a no content status code response. </returns>
    [HttpDelete("{shiftId}/job/{jobId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveJobRequirementAsync([FromRoute, Required] int shiftId, [FromRoute, Required] int jobId)
    {
        await _shiftService.DeleteJobRequirementAsync(shiftId, jobId);
        return NoContent();
    }
}