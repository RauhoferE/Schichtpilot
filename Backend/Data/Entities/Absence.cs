using System.ComponentModel.DataAnnotations;

namespace Data.Entities;

public class Absence
{
    public long Id { get; set; }
    
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending/Approved/Denied
}
