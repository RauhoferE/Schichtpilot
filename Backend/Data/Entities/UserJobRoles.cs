namespace Data.Entities;

public class UserJobRoles
{
    public int UserId { get; set; }
    public User User { get; set; }
    public int JobRoleId { get; set; }
    public JobRole JobRole { get; set; }
}