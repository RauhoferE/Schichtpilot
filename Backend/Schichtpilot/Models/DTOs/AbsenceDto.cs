namespace Schichtpilot.Models.DTOs;

using Enums;

public class AbsenceDto
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public AbsenceTypeEnum AbsenceType { get; set; }
    public string Message { get; set; } = "";
    public AbsenceStatusEnum Status { get; set; } = AbsenceStatusEnum.Pending;
    public DateTime CreatedAt { get; set; }
    public string ManagerMessage { get; set; } = "";
}
