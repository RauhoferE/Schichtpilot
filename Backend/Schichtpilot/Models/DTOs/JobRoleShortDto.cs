namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a job role, mainly used in lists.
/// </summary>
public class JobRoleShortDto
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }
}