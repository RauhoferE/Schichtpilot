using System.ComponentModel.DataAnnotations;

using Data.Entities;

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
    
    [Required, MaxLength(50)]
    public string AbsenceType { get; set; } = string.Empty; // "Vacation", "SickLeave", etc.
    
    [MaxLength(255)]
    public string Message { get; set; } = string.Empty; // optional when accepted FR142
    
    [MaxLength(25)]
    public string Status { get; set; } = "Pending"; // string in DB, enum in DTOs
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // FR158 sorting
    
    [MaxLength(255)]
    public string ManagerMessage { get; set; } = string.Empty; // FR164,168
}
