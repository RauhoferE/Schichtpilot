using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;

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
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAbsenceAsync(CreateAbsenceDto dto, long userId)
    {
        //await this._absenceService.CreateAbsenceRequestAsync(dto, userId);
        return Created();
    }
}