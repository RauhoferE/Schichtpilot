namespace Data.Entities;

public class WorkSchedule
{
    public int Id { get; set; }
    
    // Always from Sunday to Sunday
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public HashSet<ShiftAssignment> ShiftAssignments { get; set; }
    public HashSet<WorkScheduleShifts> Shifts { get; set; }
    public bool IsActive { get; set; }
    public bool IsValid { get; set; }
    public bool IsDeleted { get; set; }
}