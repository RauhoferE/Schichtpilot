namespace Data.Entities;

public class UserJobRoles
{
    public long UserId { get; set; }
    public User User { get; set; }
    public int JobRoleId { get; set; }
    public JobRole JobRole { get; set; }
    
    public HashSet<ShiftAssignment>  ShiftAssignments { get; set; }
}