using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;

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

    
    public async Task<IActionResult> CreateShiftAsync(CreateShiftDto request)
    {
        
        return Created();
    }
}