using Schichtpilot.Interfaces;
using Schichtpilot.Models.Enums;

namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Used to filter available shifts.
/// </summary>
public class ShiftFilterDto
{
    public List<DayOfWeek> WeekDays { get; set; }
    public ShiftStatusEnum Status { get; set; }
    public string? Searchstring { get; set; }
}