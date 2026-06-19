namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a shift.
/// </summary>
public class ShiftDto
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public required string ColorAsHex { get; init; }

    public List<TimeSlotDto> TimeSlots { get; init; } = new List<TimeSlotDto>();

    public List<ShiftRequirementDto> JobRequirements { get; init; } = new List<ShiftRequirementDto>();
}