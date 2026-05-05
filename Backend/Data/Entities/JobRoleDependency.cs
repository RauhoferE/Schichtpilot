namespace Data.Entities;

/// <summary>
/// Physical record of a job role dependency in the JobRoleDependencies table.
/// Linked to <see cref="JobRole"/> via JobRoleId.
/// /// Linked to <see cref="Dependency"/> via DependencyJobRoleId.
/// </summary>
public class JobRoleDependency
{
    public int JobRoleId { get; set; }
    public JobRole JobRole { get; set; }

    public int DependencyJobRoleId { get; set; }
    public JobRole Dependency { get; set; }
}