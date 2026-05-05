using System.Security.Claims;
using Data.Entities;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Responses;

namespace Schichtpilot.Interfaces;

/// <summary>
/// Orchestrates user specific operations including getting and creating users.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets a specific user id.
    /// </summary>
    /// <param name="user"> The claimsprinciple assigned to the user. </param>
    /// <returns> Returns the user id. </returns>
    Task<long> GetUserIdAsync(ClaimsPrincipal user);

    /// <summary>
    /// Creating a new user.
    /// </summary>
    /// <param name="userDto"> The new user to be created. </param>
    /// <param name="password"> The password of the new user. </param>
    /// <returns></returns>
    Task CreateUserAsync(UserDto userDto, string password);

    /// <summary>
    /// Gets user data by user id.
    /// </summary>
    /// <param name="userId"> The id of the user. </param>
    /// <returns> Returns user data as <see cref="UserDto"/>. </returns>
    Task<UserDto> GetUserDataAsync(int userId);

    /// <summary>
    /// Gets all available users.
    /// </summary>
    /// <param name="paginationDto"> The pagination element. </param>
    /// <param name="userSortingDto"> How the returned users are sorted. </param>
    /// <param name="userFilterDto"> How the returned users are filtered. </param>
    /// <returns> Returns the users as a <see cref="QueryableUserResponse"/>. </returns>
    Task<QueryableUserResponse> GetUsersAsync(PaginationDto paginationDto, UserSortingDto userSortingDto, UserFilterDto userFilterDto);
}