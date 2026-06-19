namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a shift with only id, name and color used in lists.
/// </summary>
public class ShortShiftDto
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public required string ColorAsHex { get; init; }
}