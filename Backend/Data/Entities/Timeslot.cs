using System.ComponentModel.DataAnnotations;

namespace Data.Entities;

public class Timeslot
{
    public int Id { get; set; }
    
    // Using the built-in .NET DayOfWeek enum (0 = Sunday, etc.)
    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }
    
    [Required]
    public TimeOnly EndTime { get; set; }

    public int ShiftId { get; set; }
    public Shift Shift { get; set; }
}