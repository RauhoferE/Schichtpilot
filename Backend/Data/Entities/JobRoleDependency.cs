namespace Data.Entities;

public class JobRoleDependency
{
    public int JobRoleId { get; set; }
    public JobRole JobRole { get; set; }

    public int DependencyJobRoleId { get; set; }
    public JobRole Dependency { get; set; }
}