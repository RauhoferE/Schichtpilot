using Schichtpilot.Interfaces;
using Schichtpilot.Models.Enums;

namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Used to filter schedules when requesting them.
/// </summary>
public class ScheduleFilterDot
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Searchstring { get; set; }
    public List<int> ShiftIds { get; set; } = new List<int>();
    public ScheduleStatusEnum Status { get; set; }
}