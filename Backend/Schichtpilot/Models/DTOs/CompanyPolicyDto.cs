namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents the company policy used as constraints for schedules.
/// </summary>
public class CompanyPolicyDto
{
    public required int MinimumRestPeriodInMinutes { get; set; }

    public required int RestPeriodThresholdInMinutes { get; set; }
    public required int MaximumConsecutiveWorkHoursPerDay { get; set; }
    public required int MaximumConsecutiveWorkHoursPerWeek { get; set; }
}