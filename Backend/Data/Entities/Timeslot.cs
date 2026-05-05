using System.ComponentModel.DataAnnotations;

namespace Data.Entities;

/// <summary>
/// Physical record of a timeslot in the Timeslots table.
/// Linked to <see cref="Shift"/> via ShiftId.
/// </summary>
public class Timeslot
{
    public int Id { get; set; }

    // Using the built-in .NET DayOfWeek enum (0 = Sunday, etc.)
    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    // If the EndTime goes over midnight create two timeslots like so:
    // Monday 22-24
    // Tuesday 24-02
    // And always check in teh request that start time is smaller than endtime.
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