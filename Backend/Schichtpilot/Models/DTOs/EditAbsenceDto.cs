namespace Schichtpilot.Models.DTOs;

using Schichtpilot.Models.Enums;

public class EditAbsenceDto
{
    public string? Reason { get; set; }
    
    public required AbsenceStatusEnum Status { get; set; }
}
