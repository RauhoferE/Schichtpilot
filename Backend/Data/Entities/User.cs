using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Data.Entities;

public class User : IdentityUser<long>
{
    [Required]
    public string FirstName { get; set; }
    
    [Required]
    public string LastName { get; set; }
}