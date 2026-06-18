namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents the holidays defined in the database.
/// </summary>
public class HolidaysDto
{
    public required List<DateTime> Holidays { get; init; }
}