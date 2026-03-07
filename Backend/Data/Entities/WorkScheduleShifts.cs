namespace Data.Entities;

public class WorkScheduleShifts
{
    public WorkSchedule WorkSchedule { get; set; }
    public int WorkScheduleId { get; set; }
    public Shift Shift { get; set; }
    public int ShiftId { get; set; }
}