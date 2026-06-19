namespace Schichtpilot.Models.Requests;

/// <summary>
/// Represents a request to get absences.
/// </summary>
public class GetAbsencesRequest
{
    public required int Page { get; set; }
    public required int PageSize { get; set; }
    public List<string>? Status { get; set; }
    public List<string>? AbsenceType { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public string? Searchstring { get; set; } // employee/type
}