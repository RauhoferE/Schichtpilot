namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a break in a shift.
/// </summary>
public class BreakDto
{
    public int Id { get; set; }
    public required TimeOnly StartTime { get; set; }

    public required TimeOnly EndTime { get; set; }
}