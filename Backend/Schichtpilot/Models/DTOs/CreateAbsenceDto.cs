namespace Schichtpilot.Models.DTOs;

public class CreateAbsenceDto
{
    public required long UserId  { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public string AbsenceType {get;set;} // string → entity
    public string? Message { get; set; }
    public string Status { get; set; }
}