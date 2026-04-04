namespace Schichtpilot.Models.DTOs;

public class CreateShiftDto
{
    public required string Name { get; set; }
    
    public required string ColorAsHex { get; set; }
    
    // TODO: Check the timeslots for overlapping times
    public required List<TimeSlotDto> TimeSlots { get; set; }
    
    //TODO: Check for distinct job roles
    public required List<ShiftRequirementDto> JobRequirements { get; set; }
}