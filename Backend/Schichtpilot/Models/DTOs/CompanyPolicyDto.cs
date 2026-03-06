namespace Schichtpilot.Models.DTOs;

public class CompanyPolicyDto
{
    public int RestPeriodInMinutes { get; set; }
    
    public int RestPeriodThresholdInMinutes { get; set; }
    public int MaximumConsecutiveWorkHours  { get; set; }
}