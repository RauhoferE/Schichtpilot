using System.ComponentModel.DataAnnotations;

namespace Data.Entities;
// The work policy is for all empolyees, in future maybe assign them to specific jobs or user
public class WorkPolicy
{
    public int Id { get; set; }
    
    [Required]
    public int RestPeriodInMinutes { get; set; }
    
    [Required]
    public int RestPeriodThresholdInMinutes { get; set; }
    
    [Required]
    public int MaximumConsecutiveWorkHours  { get; set; }
}