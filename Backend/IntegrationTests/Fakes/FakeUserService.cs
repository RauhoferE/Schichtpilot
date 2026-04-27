using System.Security.Claims;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Responses;

namespace IntegrationTests.Fakes;

public class FakeUserService : IUserService
{
    public long? LastGetUserIdForUserId { get; private set; }
    public UserDto? LastCreatedUserDto { get; private set; }
    public string? LastCreatedPassword { get; private set; }
    public int? LastGetUserDataUserId { get; private set; }
    public PaginationDto? LastGetUsersPagination { get; private set; }
    public UserSortingDto? LastGetUsersSorting { get; private set; }
    public UserFilterDto? LastGetUsersFilter { get; private set; }

    public Task<long> GetUserIdAsync(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (long.TryParse(claim, out var userId))
        {
            LastGetUserIdForUserId = userId;
            return Task.FromResult(userId);
        }

        LastGetUserIdForUserId = 1;
        return Task.FromResult(1L);
    }

    public Task CreateUserAsync(UserDto userDto, string password)
    {
        LastCreatedUserDto = userDto;
        LastCreatedPassword = password;
        return Task.CompletedTask;
    }

    public Task<UserDto> GetUserDataAsync(int userId)
    {
        LastGetUserDataUserId = userId;

        var user = new UserDto
        {
            Email = "user@example.com",
            FirstName = "Test",
            LastName = "User",
            Birthdate = DateTime.UtcNow.AddYears(-25),
            AddressDto = new AddressDto
            {
                Street = "Test Street 1",
                City = "Test City",
                PostalCode = 12345
            },
            AssignedJobRoles = new List<JobRoleShortDto>()
        };

        return Task.FromResult(user);
    }

    public Task<QueryableUserResponse> GetUsersAsync(PaginationDto paginationDto, UserSortingDto userSortingDto, UserFilterDto userFilterDto)
    {
        LastGetUsersPagination = paginationDto;
        LastGetUsersSorting = userSortingDto;
        LastGetUsersFilter = userFilterDto;

        var response = new QueryableUserResponse
        {
            Count = 0,
            Users = Array.Empty<UserDto>()
        };

        return Task.FromResult(response);
    }
}
