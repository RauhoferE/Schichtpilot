namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a user.
/// </summary>
public class UserDto
{
    public int Id { get; init; }
    public required string Email { get; init; }
    public required AddressDto AddressDto { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public DateTime Birthdate { get; init; }

    public List<JobRoleShortDto> AssignedJobRoles { get; init; } = new List<JobRoleShortDto>();
}