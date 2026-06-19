namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a workschedule with the essentials used in lists.
/// </summary>
public class WorkScheduleShortDto
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    public bool IsActive { get; init; }

    public bool IsValid { get; init; }

    public int ShiftCount { get; init; }
}