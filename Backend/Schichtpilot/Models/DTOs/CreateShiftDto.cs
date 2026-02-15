namespace Schichtpilot.Models.DTOs;

public class CreateShiftDto
{
    public string Name { get; set; }
    
    public string ColorAsHex { get; set; }
    
    public List<TimeSlotDto> TimeSlots { get; set; }
    
    public List<ShiftRequirementDto> JobRequirements { get; set; }
}