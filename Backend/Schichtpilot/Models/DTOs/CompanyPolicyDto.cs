namespace Schichtpilot.Models.DTOs;

public class CompanyPolicyDto
{
    public int MinimumRestPeriodInMinutes { get; set; }
    
    public int RestPeriodThresholdInMinutes { get; set; }
    public int MaximumConsecutiveWorkHoursPerDay  { get; set; }
    public int MaximumConsecutiveWorkHoursPerWeek  { get; set; }
}