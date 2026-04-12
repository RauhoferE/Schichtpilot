using System.ComponentModel.DataAnnotations;

namespace Data.Entities;

public class ShiftAssignment
{
    public Timeslot Timeslot { get; set; }

    public int TimeslotId { get; set; }

    public UserJobRoles UserJobRole { get; set; }

    public long UserId { get; set; }
    public int JobRoleId { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    public WorkSchedule WorkSchedule { get; set; }
    public int WorkScheduleId { get; set; }
}