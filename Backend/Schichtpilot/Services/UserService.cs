using AutoMapper;
using Core;
using Data.Entities;
using Microsoft.AspNetCore.Identity;
using Schichtpilot.Exceptions;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Services;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(UserManager<User> userManager, IMapper mapper, ILogger<UserService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
        
    }
}