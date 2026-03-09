namespace Schichtpilot.Models.Requests;

public class GetAbsencesRequest
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<string>? Status { get; set; }
    public List<string>? AbsenceType { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public string? Searchstring { get; set; } // employee/type
}