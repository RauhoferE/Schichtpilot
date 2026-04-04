namespace Schichtpilot.Models.DTOs;

public class GenerateScheduleDto
{
    public required string Name { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public required List<int> ShiftIds { get; set; }
}