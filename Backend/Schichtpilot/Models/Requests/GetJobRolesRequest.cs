namespace Schichtpilot.Models.Requests;

/// <summary>
/// Represents a request to get all jobroles.
/// </summary>
public class GetJobRolesRequest
{
    public required int Page { get; set; }
    public required int PageSize { get; set; }
    public string? Searchstring { get; set; }
}