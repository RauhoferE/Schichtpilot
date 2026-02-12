using System.ComponentModel.DataAnnotations;

namespace Data.Entities;

public class JobRole
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; }
    
    public string Description { get; set; }
    
    public DateTime CreatedOn { get; set; }
    
    public HashSet<UserJobRoles> UsersWithRole   {get; set;}
}