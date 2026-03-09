using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;
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
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> GenerateScheduleAsync([FromBody, Required] GenerateScheduleDto dto)
    {
        await this._workScheduleService.GenerateScheduleAsync(dto);
        return Created();
    }
    
    [HttpPost("generate/{scheduleId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GenerateScheduleAsync([FromRoute, Required] int scheduleId)
    {
        await this._workScheduleService.ReGenerateScheduleAsync(scheduleId);
        return NoContent();
    }
    
    [HttpPost("publish/{scheduleId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PublishScheduleAsync([FromRoute, Required] int scheduleId)
    {
        await this._workScheduleService.PublishScheduleAsync(scheduleId);
        return NoContent();
    }
    
    [HttpDelete("{scheduleId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteScheduleAsync([FromRoute, Required] int scheduleId)
    {
        await this._workScheduleService.DeleteScheduleAsync(scheduleId);
        return NoContent();
    }
    
    [HttpPatch("{scheduleId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateScheduleAsync([FromRoute, Required] int scheduleId, [FromBody, Required] UpdateScheduleRequest dto)
    {
        await this._workScheduleService.ChangeScheduleDateAsync(scheduleId,  dto.StartTime, dto.EndTime);
        return NoContent();
    }
    
    // [HttpPatch("{scheduleId}")]
    // [ProducesResponseType(typeof() StatusCodes.Status200OK)]
    // public async Task<IActionResult> GetScheduleAsync([FromRoute, Required] int scheduleId)
    // {
    //     await this._workScheduleService.get(scheduleId);
    //     return NoContent();
    // }
    
    [HttpPatch("{scheduleId}/inactive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetScheduleInActiveAsync([FromRoute, Required] int scheduleId)
    {
        await this._workScheduleService.SetScheduleOfflineAsync(scheduleId);
        return NoContent();
    }
    
    [HttpPatch("{scheduleId}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetScheduleAsActiveAsync([FromRoute, Required] int scheduleId)
    {
        await this._workScheduleService.SetScheduleActiveAsync(scheduleId);
        return NoContent();
    }
}