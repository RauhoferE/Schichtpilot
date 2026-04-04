namespace Schichtpilot.Models.DTOs;

public class QueryableSchedules
{
    public int Count { get; set; }
    public IEnumerable<WorkScheduleShortDto> WorkSchedules { get; set; }
}