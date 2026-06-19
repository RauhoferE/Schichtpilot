namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a user assigned to a timeslot, used by workschedules.
/// </summary>
public class AssignedUserDto
{
    public required TimeSlotDto TimeSlot { get; init; }

    public required UserDto User { get; init; }

    public required JobRoleShortDto JobRole { get; init; }

    public DateTime StartTime { get; init; }

    public DateTime EndTime { get; init; }
}