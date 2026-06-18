using System.Text.Json.Serialization;

namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a timeslot.
/// </summary>
public class TimeSlotDto
{
    public int Id { get; set; }
    
    [JsonConverter(typeof(JsonNumberEnumConverter<DayOfWeek>))]
    public DayOfWeek DayOfWeek { get; set; }

    public required TimeOnly StartTime { get; set; }

    public required TimeOnly EndTime { get; set; }

    public required List<BreakDto> Breaks { get; set; }
}