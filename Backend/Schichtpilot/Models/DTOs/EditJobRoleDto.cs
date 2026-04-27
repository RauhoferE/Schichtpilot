namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a request to edit a job role.
/// </summary>
public class EditJobRoleDto
{
    public required string Name { get; set; }

    public required string Description { get; set; }
}