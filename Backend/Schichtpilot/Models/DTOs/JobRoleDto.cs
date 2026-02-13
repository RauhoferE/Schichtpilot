namespace Schichtpilot.Models.DTOs;

public class JobRoleDto
{
    public int Id  { get; set; }
    
    public required string Name  {get; set;}
    
    public required string Description {get; set;}
    
    // Benötigt folgende Rollen
    public List<JobRoleDto> DependentOn {get; set;}
    
    // Voraussetzung für
    public List<JobRoleDto> Prerequisites {get; set;}
    
    public List<UserDto> Users {get; set;}
}