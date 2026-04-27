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
/// Provides an endpoint to register and get available users.
/// </summary>
[Controller]
[Route("api/[controller]")]
public class UserController : Controller
{
    private readonly IUserService _userService;

    private readonly IMapper _mapper;

    public UserController(IUserService userService, IMapper mapper)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// Used to register a new user.
    /// </summary>
    /// <param name="request"> The registration request. </param>
    /// <returns> Returns a created status code response. </returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateUserAsync([FromBody, Required] CreateUserRequest request)
    {
        await this._userService.CreateUserAsync(this._mapper.Map<CreateUserRequest, UserDto>(request), request.Password);
        return Created();
    }

    /// <summary>
    /// Gets specific user detail.
    /// </summary>
    /// <param name="userId"> The user id to get the specific user. </param>
    /// <returns> Returns the user data as <see cref="UserDto"/>. </returns>
    [HttpGet("{userId}")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserDataAsync([FromRoute, Required] int userId)
    {
        return Ok(await this._userService.GetUserDataAsync(userId));
    }

    /// <summary>
    /// Gets all available users.
    /// </summary>
    /// <param name="request"> The user sorting and filtering request. </param>
    /// <returns> Returns the users as <see cref="QueryableUserResponse"/>. </returns>
    [HttpGet("all")]
    [Authorize(Roles = UserRolesClass.Admin)]
    [ProducesResponseType(typeof(QueryableUserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsersAsync([FromQuery] GetUsersRequest request)
    {
        var paginationDto = this._mapper.Map<GetUsersRequest, PaginationDto>(request);
        var userSortingDto = this._mapper.Map<GetUsersRequest, UserSortingDto>(request);
        var userFilterDto = this._mapper.Map<GetUsersRequest, UserFilterDto>(request);
        return Ok(await this._userService.GetUsersAsync(paginationDto, userSortingDto, userFilterDto));
    }
}