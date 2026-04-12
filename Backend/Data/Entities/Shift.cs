using System.ComponentModel.DataAnnotations;

namespace Data.Entities;

public class Shift
{
    public int Id { get; set; }

    [Required, MaxLength(25)]
    public string Name { get; set; }

    [Required, MaxLength(6)]
    public string ColorAsHex { get; set; }

    public HashSet<Timeslot> Timeslots { get; set; }

    // Here are all the jobs required for this shift
    // This also inlcudes the dependencies
    public HashSet<ShiftRequirement> JobRequirements { get; set; }

    public HashSet<WorkScheduleShifts> ShiftAssignments { get; set; }
}