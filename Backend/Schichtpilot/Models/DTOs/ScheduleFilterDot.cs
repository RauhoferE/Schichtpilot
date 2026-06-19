using Schichtpilot.Interfaces;
using Schichtpilot.Models.Enums;

namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Used to filter schedules when requesting them.
/// </summary>
public class ScheduleFilterDot
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Searchstring { get; init; }
    public List<int> ShiftIds { get; init; } = new List<int>();
    public ScheduleStatusEnum Status { get; init; }
}