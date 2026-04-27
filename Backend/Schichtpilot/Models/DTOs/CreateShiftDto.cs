namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a request to create a new shift
/// </summary>
public class CreateShiftDto
{
    public required string Name { get; set; }

    public required string ColorAsHex { get; set; }

    public required List<TimeSlotDto> TimeSlots { get; set; }

    public required List<ShiftRequirementDto> JobRequirements { get; set; }
}