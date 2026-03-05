namespace Schichtpilot.Models.DTOs;

public class TimeSlotDto
{
    public int Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    
    public TimeOnly StartTime { get; set; }
    
    public TimeOnly EndTime { get; set; }
    
    public List<BreakDto> Breaks { get; set; }
}