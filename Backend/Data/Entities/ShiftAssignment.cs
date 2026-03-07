namespace Data.Entities;

public class ShiftAssignment
{
    public int Id { get; set; }
    
    public Timeslot Timeslot { get; set; }
    
    public int TimeslotId { get; set; }
    
    public UserJobRoles  UserJobRole { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public DateTime EndTime { get; set; }
    
    public WorkSchedule WorkSchedule { get; set; }
    public int WorkScheduleId { get; set; }
}