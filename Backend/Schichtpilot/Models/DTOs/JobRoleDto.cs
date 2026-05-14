namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a job role and all jobs it is dependent on or is a prerequisite to.
/// </summary>
public class JobRoleDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    // Needs the following roles
    public List<JobRoleShortDto> DependentOn { get; set; }

    // Role is dependent on
    public List<JobRoleShortDto> Prerequisites { get; set; }

    public List<UserDto> Users { get; set; }
}