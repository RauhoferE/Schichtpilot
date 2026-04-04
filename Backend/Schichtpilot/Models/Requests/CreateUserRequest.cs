using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Models.Requests;

public class CreateUserRequest
{
    public required string Email { get; set; }
    public required AddressDto AddressDto { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required DateTime Birthdate { get; set; }
    public required string Password { get; set; }
}