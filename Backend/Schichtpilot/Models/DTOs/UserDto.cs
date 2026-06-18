namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a user.
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required AddressDto AddressDto { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateTime Birthdate { get; set; }

    public List<JobRoleShortDto> AssignedJobRoles { get; set; }
}