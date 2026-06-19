using Schichtpilot.Interfaces;
using Schichtpilot.Models.Enums;

namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Used to filter available shifts.
/// </summary>
public class ShiftFilterDto
{
    public List<DayOfWeek> WeekDays { get; init; } = new List<DayOfWeek>();
    public ShiftStatusEnum Status { get; init; }
    public string? Searchstring { get; init; }
}