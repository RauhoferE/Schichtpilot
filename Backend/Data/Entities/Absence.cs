using System.ComponentModel.DataAnnotations;

using Data.Entities;
/// <summary>
/// The absence entity.
/// </summary>
public class Absence
{
    public int Id { get; set; }

    [Required]
    public long UserId { get; set; }
    public User User { get; set; } = null!;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public string AbsenceType { get; set; } // "Vacation", "SickLeave", etc.

    [MaxLength(250)]
    public string Message { get; set; } = string.Empty; // optional when accepted FR142

    [Required]
    public string Status { get; set; } = "Pending"; // string in DB, enum in DTOs

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // FR158 sorting

    [MaxLength(250)]
    public string ManagerMessage { get; set; } = string.Empty; // FR164,168
}
