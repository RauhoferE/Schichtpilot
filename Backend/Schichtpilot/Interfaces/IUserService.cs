using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Interfaces;

public interface IUserService
{
    Task CreateUserAsync(UserDto userDto, string password);
}