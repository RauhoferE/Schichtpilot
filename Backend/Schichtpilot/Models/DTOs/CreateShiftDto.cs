namespace Schichtpilot.Models.DTOs;

public class CreateShiftDto
{
    public string Name { get; set; }
    
    public string ColorAsHex { get; set; }
    
    // TODO: Check the timeslots for overlapping times
    public List<TimeSlotDto> TimeSlots { get; set; }
    
    //TODO: Check for distinct job roles
    public List<ShiftRequirementDto> JobRequirements { get; set; }
}