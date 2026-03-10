namespace Schichtpilot.Models.Requests;

public class GetUsersRequest
{
    public required int Page { get; set; }
    public required int PageSize { get; set; }
    public required string SortProperty { get; set; }
    public bool Ascending { get; set; }
    public string[]? JobFilters { get; set; }
    public required string AccountStatus { get; set; }
    public string? Searchstring { get; set; }
    
}