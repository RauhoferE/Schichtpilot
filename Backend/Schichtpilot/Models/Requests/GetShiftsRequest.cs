namespace Schichtpilot.Models.Requests;

/// <summary>
/// Represents a request to get work shifts.
/// </summary>
public class GetShiftsRequest
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string[] WeekDays { get; set; }
    public string ShiftStatusEnum { get; set; }
    public string? Searchstring { get; set; }
}