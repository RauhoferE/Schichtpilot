namespace Schichtpilot.Models.DTOs;

public class CompanyPolicyDto
{
    public required int MinimumRestPeriodInMinutes { get; set; }

    public required int RestPeriodThresholdInMinutes { get; set; }
    public required int MaximumConsecutiveWorkHoursPerDay { get; set; }
    public required int MaximumConsecutiveWorkHoursPerWeek { get; set; }
}