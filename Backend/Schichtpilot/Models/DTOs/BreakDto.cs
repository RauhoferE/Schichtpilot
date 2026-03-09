namespace Schichtpilot.Models.DTOs;

public class BreakDto
{
    public int Id { get; set; }
    public TimeOnly StartTime { get; set; }
    
    public TimeOnly EndTime { get; set; }
}