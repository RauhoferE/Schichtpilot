namespace Schichtpilot.Models.DTOs;

public class ShiftRequirementDto
{
    public int JobId { get; set; }
    
    public string Name { get; set; }
    
    public int RequiredStaffCount  { get; set; }
}