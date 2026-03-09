using System.ComponentModel.DataAnnotations;

namespace Data.Entities;

public class WorkSchedule
{
    public int Id { get; set; }
    
    [Required, MaxLength(25)]
    public string Name { get; set; }
    
    // Always from Sunday to Sunday
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    public HashSet<ShiftAssignment> ShiftAssignments { get; set; }
    public HashSet<WorkScheduleShifts> Shifts { get; set; }
    // Only maximum of one schedule for this startdata and enddata can be active 
    // All others are considered drafts
    public bool IsActive { get; set; }
    public bool IsValid { get; set; }
}