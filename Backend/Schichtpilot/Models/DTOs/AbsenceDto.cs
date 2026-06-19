namespace Schichtpilot.Models.DTOs;

using Enums;

/// <summary>
/// Represents an absence created by a user. 
/// </summary>
public class AbsenceDto
{
    public int Id { get; init; }
    public long UserId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public AbsenceTypeEnum AbsenceType { get; init; }
    public string Message { get; init; } = "";
    public AbsenceStatusEnum Status { get; init; } = AbsenceStatusEnum.Pending;
    public DateTime CreatedAt { get; init; }
    public string ManagerMessage { get; init; } = "";
}
