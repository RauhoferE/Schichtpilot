namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a shift.
/// </summary>
public class ShiftDto
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string ColorAsHex { get; set; }

    public List<TimeSlotDto> TimeSlots { get; set; }

    public List<ShiftRequirementDto> JobRequirements { get; set; }
}