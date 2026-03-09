using System.ComponentModel.DataAnnotations;

namespace Data.Entities;

public class ShiftRequirement
{
    public int Id { get; set; }

    public int ShiftId { get; set; }
    public Shift Shift { get; set; }

    public int JobRoleId { get; set; }
    public JobRole JobRole { get; set; }
    
    [Required]
    public int RequiredStaffCount { get; set; }
}