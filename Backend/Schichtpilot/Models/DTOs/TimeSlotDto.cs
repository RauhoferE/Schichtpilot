namespace Schichtpilot.Models.DTOs;

public class TimeSlotDto
{
    public DayOfWeek DayOfWeek { get; set; }
    
    public TimeOnly StartTime { get; set; }
    
    public TimeOnly EndTime { get; set; }
}