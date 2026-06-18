namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a break in a shift.
/// </summary>
public class BreakDto
{
    public int Id { get; init; }
    public required TimeOnly StartTime { get; init; }

    public required TimeOnly EndTime { get; init; }
}