namespace Schichtpilot.Models.DTOs;

public class TimeSlotDto
{
    public int Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    
    public required TimeOnly StartTime { get; set; }
    
    public required TimeOnly EndTime { get; set; }
    
    public required List<BreakDto> Breaks { get; set; }
}