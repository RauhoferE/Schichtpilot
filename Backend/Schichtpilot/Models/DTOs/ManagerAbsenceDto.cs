namespace Schichtpilot.Models.DTOs;

using Enums;


// Represents an absence for the manager overview, including employee name.

public class ManagerAbsenceDto
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public string EmployeeName { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public AbsenceTypeEnum AbsenceType { get; set; }
    public string Message { get; set; } = "";
    public AbsenceStatusEnum Status { get; set; } = AbsenceStatusEnum.Pending;
    public DateTime CreatedAt { get; set; }
    public string ManagerMessage { get; set; } = "";
}