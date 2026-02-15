using System.ComponentModel.DataAnnotations;

namespace Data.Entities;

public class Timeslot
{
    public int Id { get; set; }
    
    // Using the built-in .NET DayOfWeek enum (0 = Sunday, etc.)
    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    // TODO: If the EndTime goes over midnight create two timeslots like so:
    // Monday 22-24
    // Tuesday 24-02
    // And always check in teh request that start time is smaller than endtime.
    [Required]
    public TimeOnly StartTime { get; set; }
    
    [Required]
    public TimeOnly EndTime { get; set; }

    public int ShiftId { get; set; }
    public Shift Shift { get; set; }
}