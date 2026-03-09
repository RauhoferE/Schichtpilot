using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Models.Requests;

public class CreateUserRequest
{
    public required string Email { get; set; }
    public required AddressDto AddressDto { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateTime Birthdate { get; set; }
    public string Password { get; set; }
}