namespace Schichtpilot.Models.DTOs;

using Enums;


// Represents an absence for the manager overview, including employee name.

public class ManagerAbsenceDto
{
    public int Id { get; init; }
    public long UserId { get; init; }
    public string EmployeeName { get; init; } = "";
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public AbsenceTypeEnum AbsenceType { get; init; }
    public string Message { get; init; } = "";
    public AbsenceStatusEnum Status { get; init; } = AbsenceStatusEnum.Pending;
    public DateTime CreatedAt { get; init; }
    public string ManagerMessage { get; init; } = "";
}