using System.ComponentModel.DataAnnotations;

namespace Data.Entities;

/// <summary>
/// Physical record of a Break in the Breaks table.
/// Linked to <see cref="Timeslot"/> via TimeslotId.
/// </summary>
public class Break
{
    public int Id { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }

    public int TimeslotId { get; set; }
    public Timeslot Timeslot { get; set; }
}