namespace Schichtpilot.Models.Requests;

public class UpdateScheduleRequest
{
    public required DateTime StartTime { get; set; }
    public required DateTime EndTime { get; set; }
}