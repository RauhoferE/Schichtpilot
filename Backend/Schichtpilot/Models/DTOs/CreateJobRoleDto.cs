namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a request to create a new job role.
/// </summary>
public class CreateJobRoleDto
{
    public required string Name { get; set; }

    public required string Description { get; set; }

    public required List<int> DependentOnJobRoleIds { get; set; }
}