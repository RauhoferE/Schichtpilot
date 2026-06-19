using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Models.Responses;

/// <summary>
/// Represents a response that shows the shifts. 
/// </summary>
public class QueryableShiftResponse
{
    public int Count { get; init; }

    public IEnumerable<ShortShiftDto> Shift { get; init; } = [];
}