namespace Schichtpilot.Models.DTOs;

public class BreakDto
{
    public int Id { get; set; }
    public required TimeOnly StartTime { get; set; }

    public required TimeOnly EndTime { get; set; }
}