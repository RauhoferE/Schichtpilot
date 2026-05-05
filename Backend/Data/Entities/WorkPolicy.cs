using System.ComponentModel.DataAnnotations;

namespace Data.Entities;

/// <summary>
/// Physical record of a work policy in the WorkPolicy table.
/// </summary>
public class WorkPolicy
{
    public int Id { get; set; }

    [Required]
    public int MinimumRestPeriodInMinutes { get; set; }

    [Required]
    public int RestPeriodThresholdInMinutes { get; set; }// After so many minutes a break of atleast RestPeriodInMinutes must be defined

    [Required]
    public int MaximumConsecutiveWorkHoursPerDay { get; set; }

    [Required]
    public int MaximumConsecutiveWorkHoursPerWeek { get; set; }

    // Optional: Maybe add multiple policies and then let the user choose which one
}