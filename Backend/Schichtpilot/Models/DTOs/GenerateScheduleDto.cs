namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a request to generate a schedule.
/// </summary>
public class GenerateScheduleDto
{
    public required string Name { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public required List<int> ShiftIds { get; set; }
}