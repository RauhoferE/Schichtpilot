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
public class UserController : Controller
{
    private readonly IUserService _userService;
    
    private readonly IMapper _mapper;

    public UserController(IUserService userService, IMapper mapper)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateUserAsync([FromBody, Required] CreateUserRequest request)
    {
        await this._userService.CreateUserAsync(this._mapper.Map<CreateUserRequest, UserDto>(request), request.Password);
        return Created();
    }

    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserDataAsync([FromRoute, Required] int userId)
    {
        return Ok(await this._userService.GetUserDataAsync(userId));
    }
    
    [HttpGet("all")]
    [ProducesResponseType(typeof(QueryableUserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsersAsync([FromQuery] GetUsersRequest request)
    {
        var paginationDto = this._mapper.Map<GetUsersRequest, PaginationDto>(request);
        var userSortingDto = this._mapper.Map<GetUsersRequest, UserSortingDto>(request);
        var userFilterDto = this._mapper.Map<GetUsersRequest, UserFilterDto>(request);
        return Ok(await this._userService.GetUsersAsync(paginationDto, userSortingDto, userFilterDto));
    }
}