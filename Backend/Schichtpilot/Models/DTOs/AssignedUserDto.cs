namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a user assigned to a timeslot, used by workschedules.
/// </summary>
public class AssignedUserDto
{
    public TimeSlotDto TimeSlot { get; set; }

    public UserDto User { get; set; }

    public JobRoleShortDto JobRole { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }
}