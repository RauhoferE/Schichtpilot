using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Requests;
using Schichtpilot.Models.Responses;

namespace Schichtpilot.Controllers;

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

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateShiftAsync(CreateShiftDto request)
    {
        await  _shiftService.CreateShiftAsync(request);
        return Created();
    }
    
    [HttpGet("{shiftId}")]
    [ProducesResponseType(typeof(ShiftDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShiftAsync([FromRoute, Required]int shiftId)
    {
        return Ok(await _shiftService.GetShiftAsync(shiftId));
    }
    
    [HttpGet("all")]
    [ProducesResponseType(typeof(QueryableShiftResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShiftsAsync([FromBody, Required] GetShiftsRequest request)
    {
        var paginationDto = this._mapper.Map<GetShiftsRequest, PaginationDto>(request);
        var shiftFilterDto = this._mapper.Map<GetShiftsRequest, ShiftFilterDto>(request);
        return Ok(await _shiftService.GetShiftsAsync(paginationDto, shiftFilterDto));
    }
    
    [HttpPut("{shiftId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateShiftAsync([FromRoute, Required]int shiftId,  EditShiftDto request)
    {
        await  _shiftService.ManageShiftAsync(shiftId, request);
        return NoContent();
    }
    
    [HttpDelete("{shiftId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteShiftAsync([FromRoute, Required]int shiftId)
    {
        await  _shiftService.DeleteShiftAsync(shiftId);
        return NoContent();
    }
    
    [HttpDelete("{shiftId}/timeslot/{timeSlotId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTimeslotAsync([FromRoute, Required]int shiftId, [FromRoute, Required]int timeSlotId)
    {
        await  _shiftService.DeleteTimeSlotAsync(shiftId, timeSlotId);
        return NoContent();
    }
    
    [HttpPost("{shiftId}/timeslot")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddTimeslotAsync([FromRoute, Required]int shiftId, [FromBody, Required]TimeSlotDto timeSlotDto)
    {
        await  _shiftService.AddTimeSlotAsync(shiftId, timeSlotDto);
        return NoContent();
    }
    
    [HttpPut("{shiftId}/timeslot/{timeSlotId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateTimeslotAsync([FromRoute, Required]int shiftId, [FromBody, Required]TimeSlotDto timeSlotDto, [FromRoute, Required]int timeSlotId)
    {
        timeSlotDto.Id = timeSlotId;
        await  _shiftService.EditTimeSlotAsync(shiftId, timeSlotDto);
        return NoContent();
    }
    
    [HttpPost("{shiftId}/job")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddJobRequirementAsync([FromRoute, Required]int shiftId, [FromBody, Required]ShiftRequirementDto jobrequirementRequest)
    {
        await  _shiftService.AddJobRequirementAsync(shiftId, jobrequirementRequest);
        return NoContent();
    }
    
    [HttpPatch("{shiftId}/job/{jobId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangeJobRequirementCountAsync([FromRoute, Required]int shiftId, [FromRoute, Required]int jobId, [FromQuery, Required]int staffCount)
    {
        if (staffCount < 1)
        {
            throw new Exception("StaffCount must be greater than zero"); 
        }
        
        await  _shiftService.ChangeRequiredStaffAsync(shiftId, jobId, staffCount);
        return NoContent();
    }
    
    [HttpDelete("{shiftId}/job/{jobId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveJobRequirementAsync([FromRoute, Required]int shiftId, [FromRoute, Required]int jobId)
    {
        await  _shiftService.DeleteJobRequirementAsync(shiftId, jobId);
        return NoContent();
    }
}