namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a request to edit a shift.
/// </summary>
public class EditShiftDto
{
    public string Name { get; set; }

    public string ColorAsHex { get; set; }
}