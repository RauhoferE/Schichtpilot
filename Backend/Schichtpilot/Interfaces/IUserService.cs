using Data.Entities;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Responses;

namespace Schichtpilot.Interfaces;

public interface IUserService
{
    Task CreateUserAsync(UserDto userDto, string password);

    Task<UserDto> GetUserDataAsync(int userId);

    Task<QueryableUserResponse> GetUsersAsync(PaginationDto paginationDto, UserSortingDto userSortingDto, UserFilterDto userFilterDto);
}