namespace Schichtpilot.Models.DTOs;

public class QueryableJobRoleResponse
{
    public int Count { get; set; }
    public IEnumerable<JobRoleShortDto> JobRoles { get; set; }
}