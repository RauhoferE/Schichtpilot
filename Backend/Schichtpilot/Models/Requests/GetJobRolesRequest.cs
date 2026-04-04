namespace Schichtpilot.Models.Requests;

public class GetJobRolesRequest
{
    public required int Page { get; set; }
    public required int PageSize { get; set; }
    public string? Searchstring { get; set; }
}