namespace Schichtpilot.Models.DTOs;

using Schichtpilot.Models.Enums;

public class EditAbsenceDto
{
    public AbsenceTypeEnum? AbsenceTypeEnum { get; set; }
    public string? Message { get; set; }
}
