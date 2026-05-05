namespace Schichtpilot.Models.Requests;

/// <summary>
/// Represents a request to update a work schedule with a new time.
/// </summary>
public class UpdateScheduleRequest
{
    public required DateTime StartTime { get; set; }
    public required DateTime EndTime { get; set; }
}