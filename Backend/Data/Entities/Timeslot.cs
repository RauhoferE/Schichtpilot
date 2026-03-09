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
    // TODO: Check that its only one timeslot per day!!!!
    // TODO: Timeslots can only be 20 hours long!!!! or put it in company policy?
    [Required]
    public TimeOnly StartTime { get; set; }
    
    [Required]
    public TimeOnly EndTime { get; set; }

    public int ShiftId { get; set; }
    public Shift Shift { get; set; }
    
    public HashSet<Break> Breaks { get; set; }
    
    public HashSet<ShiftAssignment> ShiftAssignments { get; set; }
    
    // public int ShiftAssignmentId { get; set; }
    //
    // public ShiftAssignment ShiftAssignment { get; set; }
}