namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a response to get a list of workschedules.
/// </summary>
public class QueryableSchedules
{
    public int Count { get; set; }
    public IEnumerable<WorkScheduleShortDto> WorkSchedules { get; set; }
}