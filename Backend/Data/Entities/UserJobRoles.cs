namespace Data.Entities;

/// <summary>
/// Physical record of user jobroles in UserJobRoles table.
/// Linked to <see cref="User"/> via UserId.
/// Linked to <see cref="JobRole"/> via JobRoleId.
/// </summary>
public class UserJobRoles
{
    public long UserId { get; set; }
    public User User { get; set; }
    public int JobRoleId { get; set; }
    public JobRole JobRole { get; set; }

    public HashSet<ShiftAssignment> ShiftAssignments { get; set; }
}