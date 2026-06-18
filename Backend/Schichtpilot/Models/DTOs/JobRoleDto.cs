namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a job role and all jobs it is dependent on or is a prerequisite to.
/// </summary>
public class JobRoleDto
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    // Needs the following roles
    public List<JobRoleShortDto> DependentOn { get; init; } = new List<JobRoleShortDto>();

    // Role is dependent on
    public List<JobRoleShortDto> Prerequisites { get; init; } = new List<JobRoleShortDto>();

    public List<UserDto> Users { get; init; } = new List<UserDto>();
}