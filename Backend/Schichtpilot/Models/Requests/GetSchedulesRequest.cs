namespace Schichtpilot.Models.Requests;

public class GetSchedulesRequest
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Searchstring { get; set; }
    public List<int>? ShiftIds { get; set; }
    public string Status { get; set; }
}