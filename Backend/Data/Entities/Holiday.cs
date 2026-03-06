using System.ComponentModel.DataAnnotations;

namespace Data.Entities;

public class Holiday
{
    public int Id { get; set; }
    
    [Required]
    public DateTime Date { get; set; }
}