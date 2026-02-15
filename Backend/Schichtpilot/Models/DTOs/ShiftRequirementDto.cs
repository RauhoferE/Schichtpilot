namespace Schichtpilot.Models.DTOs;

public class ShiftRequirementDto
{
    public int JobId { get; set; }
    
    public string Name { get; set; }
    
    // TODO: This needs to be bigger than 0.
    public int RequiredStaffCount  { get; set; }
}