using System.ComponentModel.DataAnnotations;

namespace Data.Entities;

/// <summary>
/// Physical record of a job role in the JobRoles table.
/// </summary>
public class JobRole
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; }

    [Required, MaxLength(250)]
    public string Description { get; set; }

    [Required]
    public DateTime CreatedOn { get; set; }

    public HashSet<UserJobRoles> UsersWithRole { get; set; }

    public HashSet<JobRoleDependency> Dependencies { get; set; }

    public HashSet<JobRoleDependency> Prerequisites { get; set; }
}