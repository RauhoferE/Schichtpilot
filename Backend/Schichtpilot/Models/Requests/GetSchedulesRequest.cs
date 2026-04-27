namespace Schichtpilot.Models.Requests;

/// <summary>
/// Represents a request to get work schedules.
/// </summary>
public class GetSchedulesRequest
{
    public required int Page { get; set; }
    public required int PageSize { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Searchstring { get; set; }
    public List<int>? ShiftIds { get; set; }
    public required string Status { get; set; }
}