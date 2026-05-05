namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a shift with only id, name and color used in lists.
/// </summary>
public class ShortShiftDto
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string ColorAsHex { get; set; }
}