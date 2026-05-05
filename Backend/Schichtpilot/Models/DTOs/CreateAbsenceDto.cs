namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a request to create a new absence by a user.
/// </summary>
public class CreateAbsenceDto
{
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public required string AbsenceType { get; set; } // string → entity
    public string? Message { get; set; }
}