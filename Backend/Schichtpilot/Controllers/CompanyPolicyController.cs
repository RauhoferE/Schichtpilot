using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Controllers;

[Controller]
[Route("api/[controller]")]
public class CompanyPolicyController : Controller
{
    public CompanyPolicyController(ICompanyPolicyService companyService)
    {
        _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));
    }

    private readonly ICompanyPolicyService _companyService;
    
    [HttpGet("holidays")]
    [Authorize]
    [ProducesResponseType( typeof(HolidaysDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHolidayAsync()
    {
        return Ok(await _companyService.GetHolidaysAsync());
    }

    [HttpPost("holidays")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddHolidaysAsync([FromBody, Required] HolidaysDto holidays)
    {
        await this._companyService.AddHolidaysAsync(holidays);
        return NoContent();
    }
    
    [HttpDelete("holidays")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveHolidaysAsync([FromBody, Required] HolidaysDto holidays)
    {
        await this._companyService.RemoveHolidaysAsync(holidays);
        return NoContent();
    }
    
    [HttpPut]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetPolicyAsync([FromBody, Required] CompanyPolicyDto policy)
    {
        await this._companyService.SetPolicyAsync(policy);
        return NoContent();
    }
    
    [HttpGet]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType( typeof(CompanyPolicyDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPolicyAsync()
    {
        return Ok(await this._companyService.GetPolicyAsync());
    }
}