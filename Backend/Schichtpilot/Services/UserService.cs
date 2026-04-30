using System.Security.Claims;
using AutoMapper;
using Core;
using Data;
using Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Schichtpilot.Exceptions;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Enums;
using Schichtpilot.Models.Responses;

namespace Schichtpilot.Services;

/// <summary>
/// Orchestrates user specific operations including getting and creating users.
/// </summary>
public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;
    private readonly SchichtpilotDbContext _dbContext;
    private readonly IEmailService _emailService;

    public UserService(UserManager<User> userManager, IMapper mapper, ILogger<UserService> logger,
        SchichtpilotDbContext dbContext, IEmailService emailService)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
    }


    /// <summary>
    /// Gets a specific user id.
    /// </summary>
    /// <param name="user"> The claimsprinciple assigned to the user. </param>
    /// <returns> Returns the user id. </returns>
    /// <exception cref="Exception"> Thrown when the user could not be found. </exception>
    public async Task<long> GetUserIdAsync(ClaimsPrincipal user)
    {
        var userEntity = await this._userManager.GetUserAsync(user);

        if (userEntity == null)
        {
            throw new Exception("Error when getting user");
        }

        return userEntity.Id;
    }

    /// <summary>
    /// Creating a new user.
    /// </summary>
    /// <param name="userDto"> The new user to be created. </param>
    /// <param name="password"> The password of the new user. </param>
    /// <returns></returns>
    /// <exception cref="AccountCreationException"> Thrown when the user account could not be created. </exception>
    public async Task CreateUserAsync(UserDto userDto, string password)
    {
        var userToCreate = this._mapper.Map<UserDto, User>(userDto);

        if (await this._userManager.FindByEmailAsync(userToCreate.Email) != null)
        {
            this._logger.LogWarning($"User already exists for: {userToCreate.Email}");
            return;
        }

        var creatUserResult = await this._userManager.CreateAsync(userToCreate, password);
        if (!creatUserResult.Succeeded)
        {
            throw new AccountCreationException($"Couldn't create user: {creatUserResult.Errors.Select(error => error.Description)}");
        }

        var addUserToRoleResult = await this._userManager.AddToRoleAsync(userToCreate, UserRolesClass.User);
        if (!addUserToRoleResult.Succeeded)
        {
            throw new AccountCreationException($"Couldn't add user to user role: {creatUserResult.Errors.Select(error => error.Description)}");
        }

        _ = Task.Run(async () =>
            await _emailService.SendUserRegisterMail(userToCreate));
    }

    /// <summary>
    /// Gets user data by user id.
    /// </summary>
    /// <param name="userId"> The id of the user. </param>
    /// <returns> Returns user data as <see cref="UserDto"/>. </returns>
    /// <exception cref="UserNotFoundException"> Thrown when the user could not be found. </exception>
    public Task<UserDto> GetUserDataAsync(int userId)
    {
        //TODO: Return schedules of users
        var user = this._dbContext.Users
            .Include(x => x.JobRoles)
            .ThenInclude(x => x.JobRole)
            .FirstOrDefault(x => x.Id == userId);

        if (user == null)
        {
            throw new UserNotFoundException();
        }

        return Task.FromResult(this._mapper.Map<User, UserDto>(user));
    }

    /// <summary>
    /// Gets all available users.
    /// </summary>
    /// <param name="paginationDto"> The pagination element. </param>
    /// <param name="userSortingDto"> How the returned users are sorted. </param>
    /// <param name="userFilterDto"> How the returned users are filtered. </param>
    /// <returns> Returns the users as a <see cref="QueryableUserResponse"/>. </returns>
    public async Task<QueryableUserResponse> GetUsersAsync(PaginationDto paginationDto, UserSortingDto userSortingDto, UserFilterDto? userFilterDto)
    {
        IQueryable<User> users = this._dbContext.Users
            .Include(x => x.JobRoles)
            .ThenInclude(x => x.JobRole)
            .AsQueryable();

        if (userFilterDto != null)
        {
            users = await this.FilterUsersAsync(users, userFilterDto);
        }

        users = await this.SortUsersAsync(users, userSortingDto);

        return new QueryableUserResponse()
        {
            Count = users.Count(),
            Users = users
                .Skip((paginationDto.Page - 1) * paginationDto.PageSize)
                .Take(paginationDto.PageSize)
                .Select(x => this._mapper.Map<User, UserDto>(x))
        };
    }

    /// <summary>
    /// Sorts the given user list.
    /// </summary>
    /// <param name="users"> The users to be sorted. </param>
    /// <param name="userSortingDto"> How the users should be sorted. </param>
    /// <returns> Returns the users as <see cref="IQueryable"/>. </returns>
    /// <exception cref="ArgumentOutOfRangeException"> Thrown when the sorting enum value could not be found. </exception>
    private Task<IQueryable<User>> SortUsersAsync(IQueryable<User> users, UserSortingDto userSortingDto)
    {
        if (userSortingDto.Ascending)
        {
            switch (userSortingDto.SortProperty)
            {
                case UserSortEnum.Id:
                    users = users.OrderBy(x => x.Id);
                    break;
                case UserSortEnum.FirstName:
                    users = users.OrderBy(x => x.FirstName);
                    break;
                case UserSortEnum.LastName:
                    users = users.OrderBy(x => x.LastName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return Task.FromResult(users);
        }

        switch (userSortingDto.SortProperty)
        {
            case UserSortEnum.Id:
                users = users.OrderByDescending(x => x.Id);
                break;
            case UserSortEnum.FirstName:
                users = users.OrderByDescending(x => x.FirstName);
                break;
            case UserSortEnum.LastName:
                users = users.OrderByDescending(x => x.LastName);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return Task.FromResult(users);
    }

    /// <summary>
    /// Filters the given user list.
    /// </summary>
    /// <param name="users"> The users to be filtered. </param>
    /// <param name="userFilterDto"> How the users should be filtered. </param>
    /// <returns> Returns the users as <see cref="IQueryable"/>. </returns>
    /// <exception cref="ArgumentOutOfRangeException"> Thrown when the filter enum could not be found. </exception>
    private Task<IQueryable<User>> FilterUsersAsync(IQueryable<User> users, UserFilterDto userFilterDto)
    {
        if (userFilterDto.JobFilters.Length > 0)
        {
            foreach (var job in userFilterDto.JobFilters)
            {
                users = users
                    .Where(u => u.JobRoles.Any(jr => jr.JobRole.Name.ToLower() == job.ToLower()));
            }
        }

        if (!string.IsNullOrEmpty(userFilterDto.Searchstring))
        {
            users = users.Where(x => x.Email.ToLower().Contains(userFilterDto.Searchstring.ToLower()) ||
                                     x.LastName.ToLower().Contains(userFilterDto.Searchstring.ToLower()));
        }

        switch (userFilterDto.AccountStatus)
        {
            case AccountStatusEnum.None:
                break;
            case AccountStatusEnum.EmailVerified:
                users = users.Where(x => x.EmailConfirmed);
                break;
            case AccountStatusEnum.EmailNotVerified:
                users = users.Where(x => !x.EmailConfirmed);
                break;
            case AccountStatusEnum.Locked:
                users = users.Where(x => x.LockoutEnd.HasValue);
                break;
            case AccountStatusEnum.Ok:
                users = users.Where(x => !x.LockoutEnd.HasValue && x.EmailConfirmed);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return Task.FromResult(users);
    }
}