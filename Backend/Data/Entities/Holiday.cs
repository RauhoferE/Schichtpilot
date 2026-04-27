using System.ComponentModel.DataAnnotations;

namespace Data.Entities;

/// <summary>
/// Physical record of a Holiday entity in the Holidays table.
/// </summary>
public class Holiday
{
    public int Id { get; set; }

    [Required]
    public DateTime Date { get; set; }
}