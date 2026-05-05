using System.ComponentModel.DataAnnotations;

namespace Data.Entities;

/// <summary>
/// Physical record of a shift requirement in the ShiftRequirement table.
/// Linked to <see cref="Shift"/> via ShiftId.
/// Linked to <see cref="JobRole"/> via JobRoleId.
/// </summary>
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