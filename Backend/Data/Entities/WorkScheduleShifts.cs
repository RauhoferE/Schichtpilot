namespace Data.Entities;

/// <summary>
/// Physical record of a work schedule shift in the WorkScheduleShifts table.
/// Linked to <see cref="WorkSchedule"/> via WorkScheduleId.
/// Linked to <see cref="Shift"/> via ShiftId.
/// </summary>
public class WorkScheduleShifts
{
    public WorkSchedule WorkSchedule { get; set; }
    public int WorkScheduleId { get; set; }
    public Shift Shift { get; set; }
    public int ShiftId { get; set; }
}