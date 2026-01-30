using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Data.Entities;

public class User : IdentityUser<long>
{
    [Required, MaxLength(20)]
    public string FirstName { get; set; }
    
    [Required, MaxLength(20)]
    public string LastName { get; set; }
    
    [Required, MaxLength(50)]
    public string StreetAddress { get; set; }
    
    [Required, MaxLength(20)]
    public string City { get; set; } 
    
    [Required]
    public int PostalCode { get; set; }
    
    [Required]
    public DateTime BirthDate { get; set; }
    
    public HashSet<UserJobRoles> JobRoles { get; set; }
}