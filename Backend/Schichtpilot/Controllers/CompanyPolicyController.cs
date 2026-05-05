using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Controllers;

/// <summary>
/// Provides an endpoint to get and manage the company policy.
/// Also contains the endpoint to get, add and delete public holidays defined by the managers. 
/// </summary>
[Controller]
[Route("api/[controller]")]
public class CompanyPolicyController : Controller
{
    public CompanyPolicyController(ICompanyPolicyService companyService)
    {
        _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));
    }

    private readonly ICompanyPolicyService _companyService;

    /// <summary>
    /// Gets all defined holidays.
    /// </summary>
    /// <returns> Returns the holidays as a <see cref="HolidaysDto"/>. </returns>
    [HttpGet("holidays")]
    [Authorize]
    [ProducesResponseType(typeof(HolidaysDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHolidayAsync()
    {
        return Ok(await _companyService.GetHolidaysAsync());
    }

    /// <summary>
    /// Adds new holidays.
    /// </summary>
    /// <param name="holidays"> The new holidays to be added. </param>
    /// <returns> Returns a no content response. </returns>
    [HttpPost("holidays")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddHolidaysAsync([FromBody, Required] HolidaysDto holidays)
    {
        await this._companyService.AddHolidaysAsync(holidays);
        return NoContent();
    }

    /// <summary>
    /// Removes previously defined holidays.
    /// </summary>
    /// <param name="holidays"> The holidays to be removed. </param>
    /// <returns> Returns a no content response. </returns>
    [HttpDelete("holidays")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveHolidaysAsync([FromBody, Required] HolidaysDto holidays)
    {
        await this._companyService.RemoveHolidaysAsync(holidays);
        return NoContent();
    }

    /// <summary>
    /// Sets the company policy used as constraints for schedules and timeslots.
    /// </summary>
    /// <param name="policy"> The new company policy paramters. </param>
    /// <returns> Returns a no content response. </returns>
    [HttpPut]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetPolicyAsync([FromBody, Required] CompanyPolicyDto policy)
    {
        await this._companyService.SetPolicyAsync(policy);
        return NoContent();
    }

    /// <summary>
    /// Gets the currently defined company policy.
    /// </summary>
    /// <returns> Returns the company policy as a <see cref="CompanyPolicyDto"/>. </returns>
    [HttpGet]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(typeof(CompanyPolicyDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPolicyAsync()
    {
        return Ok(await this._companyService.GetPolicyAsync());
    }
}