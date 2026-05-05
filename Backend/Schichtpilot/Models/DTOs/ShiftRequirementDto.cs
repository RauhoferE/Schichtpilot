namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a needed job for a shift.
/// </summary>
public class ShiftRequirementDto
{
    public int JobId { get; set; }

    public string Name { get; set; }

    public required int RequiredStaffCount { get; set; }
}