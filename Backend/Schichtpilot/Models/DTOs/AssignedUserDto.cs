namespace Schichtpilot.Models.DTOs;

public class AssignedUserDto
{
    public TimeSlotDto TimeSlot { get; set; }

    public UserDto User { get; set; }

    public JobRoleShortDto JobRole { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }
}