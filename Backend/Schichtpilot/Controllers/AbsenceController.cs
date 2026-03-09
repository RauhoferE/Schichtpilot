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
public class AbsenceController : Controller
{
    public AbsenceController(IAbsenceService absenceService, IMapper mapper)
    {
        _absenceService = absenceService ?? throw new ArgumentNullException(nameof(absenceService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    private readonly IAbsenceService _absenceService;
    private readonly IMapper _mapper;

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAbsenceAsync([FromBody, Required]CreateAbsenceDto dto)
    {
        await this._absenceService.CreateAbsenceRequestAsync(dto);
        return Created();
    }
    
    [HttpDelete("{absenceId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAbsenceAsync([FromRoute, Required]int absenceId)
    {
        //TODO: Add correct user Id via authentication
        await this._absenceService.DeleteOwnAbsenceAsync(absenceId, 0);
        return NoContent();
    }
    
    [HttpPut("{absenceId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAbsenceAsync([FromRoute, Required]int absenceId, [FromBody, Required]StatusUpdateDto dto)
    {
        await this._absenceService.UpdateAbsenceStatusAsync(absenceId, dto);
        return NoContent();
    }
    
    [HttpGet("{absenceId}")]
    [ProducesResponseType( typeof(AbsenceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAbsenceAsync([FromRoute, Required]int absenceId)
    {
        return Ok(await this._absenceService.GetAbsenceDetailAsync(absenceId));
    }
    
    [HttpGet("user")]
    [ProducesResponseType( typeof(QueryableAbsenceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserAbsencesAsync([FromQuery, Required]GetAbsencesRequest request)
    {
        var paginationDto = _mapper.Map<PaginationDto>(request);
        var filterDto =  _mapper.Map<AbsenceFilterDto>(request);
        //TODO: Add userid from auth
        return Ok(await this._absenceService.ViewUserAbsencesAsync(paginationDto, filterDto, 0));
    }
    
    [HttpGet("all")]
    [ProducesResponseType( typeof(QueryableAbsenceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAbsencesAsync([FromQuery, Required]GetAbsencesRequest request)
    {
        var paginationDto = _mapper.Map<PaginationDto>(request);
        var filterDto =  _mapper.Map<AbsenceFilterDto>(request);
        return Ok(await this._absenceService.ViewAllAbsencesAsync(paginationDto, filterDto));
    }
}