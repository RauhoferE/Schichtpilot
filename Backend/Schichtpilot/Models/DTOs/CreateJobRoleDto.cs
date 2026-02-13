namespace Schichtpilot.Models.DTOs;

public class CreateJobRoleDto
{
    public required string Name  {get; set;}
    
    public required string Description {get; set;}
    
    public List<int> DependentOnJobRoleIds {get; set;}
}