namespace Schichtpilot.Models.Requests;

public class GetJobRolesRequest
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string? Searchstring { get; set; }
}