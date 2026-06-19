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
/// Provides an endpoint to Create, manage, delete and get absences.
/// </summary>
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

    /// <summary>
    /// Creates a new absence.
    /// </summary>
    /// <param name="dto"> The absence request with all needed information. </param>
    /// <returns> Returns a created status code. </returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAbsenceAsync([FromBody, Required] CreateAbsenceDto dto)
    {
        await this._absenceService.CreateAbsenceRequestAsync(this.GetUserIdFromContext(), dto);
        return Created();
    }

    /// <summary>
    /// Deletes an absence.
    /// </summary>
    /// <param name="absenceId"> The id of the absence to delete. </param>
    /// <returns> Returns no content status code. </returns>
    [HttpDelete("{absenceId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAbsenceAsync([FromRoute, Required] int absenceId)
    {
        await this._absenceService.DeleteOwnAbsenceAsync(absenceId, this.GetUserIdFromContext());
        return NoContent();
    }

    /// <summary>
    /// Updates an available absence.
    /// </summary>
    /// <param name="absenceId"> The id of the absence to update. </param>
    /// <param name="dto"> Contains the new status of the absence. </param>
    /// <returns> Returns no content status code. </returns>
    [HttpPut("{absenceId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAbsenceAsync([FromRoute, Required] int absenceId, [FromBody, Required] StatusUpdateDto dto)
    {
        await this._absenceService.UpdateAbsenceStatusAsync(absenceId, dto);
        return NoContent();
    }

    /// <summary>
    /// Gets an absences.
    /// </summary>
    /// <param name="absenceId"> The id of the absence to get. </param>
    /// <returns> Returns the absence as a <see cref="AbsenceDto"/> </returns>
    [HttpGet("{absenceId}")]
    [Authorize]
    [ProducesResponseType(typeof(AbsenceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAbsenceAsync([FromRoute, Required] int absenceId)
    {
        return Ok(await this._absenceService.GetAbsenceDetailAsync(absenceId));
    }

    /// <summary>
    /// Gets all available absences from the signed in user.
    /// </summary>
    /// <param name="request"> The absence sorting and filtering details. </param>
    /// <returns> Returns the absences as <see cref="QueryableAbsenceResponse"/> </returns>
    [HttpGet("user")]
    [Authorize]
    [ProducesResponseType(typeof(QueryableAbsenceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserAbsencesAsync([FromQuery, Required] GetAbsencesRequest request)
    {
        var paginationDto = _mapper.Map<PaginationDto>(request);
        var filterDto = _mapper.Map<AbsenceFilterDto>(request);
        return Ok(await this._absenceService.ViewUserAbsencesAsync(paginationDto, filterDto, this.GetUserIdFromContext()));
    }

    /// <summary>
    /// Gets all available absences from all users.
    /// </summary>
    /// <param name="request">The absence sorting and filtering details.</param>
    /// <returns> Returns the absences as <see cref="QueryableAbsenceResponse"/> </returns>
    [HttpGet("all")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(typeof(QueryableManagerAbsenceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAbsencesAsync([FromQuery, Required] GetAbsencesRequest request)
    {
        var paginationDto = _mapper.Map<PaginationDto>(request);
        var filterDto = _mapper.Map<AbsenceFilterDto>(request);
        return Ok(await this._absenceService.ViewAllAbsencesAsync(paginationDto, filterDto));
    }

    /// <summary>
    /// This method gets the user id from the current htto context.
    /// </summary>
    /// <returns> Returns the user id. </returns>
    private long GetUserIdFromContext()
    {
        var userIdObject = HttpContext.Items["UserId"] ?? 0;
        return Convert.ToInt64(userIdObject);
    }
}