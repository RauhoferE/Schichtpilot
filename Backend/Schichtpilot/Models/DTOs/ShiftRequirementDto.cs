namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a needed job for a shift.
/// </summary>
public class ShiftRequirementDto
{
    public int JobId { get; init; }

    public required string Name { get; init; }

    public required int RequiredStaffCount { get; set; }
}