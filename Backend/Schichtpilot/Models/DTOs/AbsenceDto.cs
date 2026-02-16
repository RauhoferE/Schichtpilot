namespace Schichtpilot.Models.DTOs;

using Schichtpilot.Models.Enums;

public class AbsenceDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public AbsenceStatusEnum Status { get; set; }
}
