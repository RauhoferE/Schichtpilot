namespace Schichtpilot.Models.DTOs;

public class CreateJobRoleDto
{
    public required string Name  {get; set;}
    
    public required string Description {get; set;}
    
    //TODO: Check for distinct job roles
    public List<int> DependentOnJobRoleIds {get; set;}
}