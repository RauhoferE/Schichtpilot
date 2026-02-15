using Schichtpilot.Interfaces;
using Schichtpilot.Models.Enums;

namespace Schichtpilot.Models.DTOs;

public class ShiftFilterDto
{
    public List<DayOfWeek> WeekDays { get; set; }
    public ShiftStatusEnum Status { get; set; }
    public string? Searchstring { get; set; }
}