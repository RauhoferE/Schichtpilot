namespace Schichtpilot.Models.DTOs;

public class WorkScheduleDto
{
    public int Id { get; set; }
    
    public string Name { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    public bool IsActive { get; set; }
    
    public bool IsValid { get; set; }
    
    public List<AssignedUserDto> AssignedUsers { get; set; }
    
    public List<ShiftDto> Shifts { get; set; }
}