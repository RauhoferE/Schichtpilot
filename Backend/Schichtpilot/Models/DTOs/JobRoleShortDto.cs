namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a job role, mainly used in lists.
/// </summary>
public class JobRoleShortDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }
}