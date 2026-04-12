namespace Schichtpilot.Models.DTOs;

public class CreateAbsenceDto
{
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public required string AbsenceType { get; set; } // string → entity
    public string? Message { get; set; }
}