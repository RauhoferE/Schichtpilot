namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a request to edit a shift.
/// </summary>
public class EditShiftDto
{
    public required string Name { get; init; }

    public required string ColorAsHex { get; init; }
}