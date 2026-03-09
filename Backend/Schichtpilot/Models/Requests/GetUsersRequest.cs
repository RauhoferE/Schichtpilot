namespace Schichtpilot.Models.Requests;

public class GetUsersRequest
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string SortProperty { get; set; }
    public bool Ascending { get; set; }
    public string[] JobFilters { get; set; }
    public string AccountStatus { get; set; }
    public string? Searchstring { get; set; }
    
}