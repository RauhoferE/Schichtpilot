using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Models.Responses;

public class QueryableShiftResponse
{
    public int Count { get; set; }
    
    public IEnumerable<ShortShiftDto> Shift { get; set; }
}