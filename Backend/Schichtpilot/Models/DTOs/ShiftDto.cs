namespace Schichtpilot.Models.DTOs;

public class ShiftDto
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string ColorAsHex { get; set; }

    public List<TimeSlotDto> TimeSlots { get; set; }

    public List<ShiftRequirementDto> JobRequirements { get; set; }
}