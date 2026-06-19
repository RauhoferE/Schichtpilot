namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a work schedule.
/// </summary>
public class WorkScheduleDto
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    public bool IsActive { get; init; }

    public bool IsValid { get; init; }

    public List<AssignedUserDto> AssignedUsers { get; init; } = new List<AssignedUserDto>();

    public List<ShiftDto> Shifts { get; init; } = new List<ShiftDto>();
}