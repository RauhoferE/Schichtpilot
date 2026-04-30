namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a response that contains available job roles.
/// </summary>
public class QueryableJobRoleResponse
{
    public int Count { get; set; }
    public IEnumerable<JobRoleShortDto> JobRoles { get; set; }
}