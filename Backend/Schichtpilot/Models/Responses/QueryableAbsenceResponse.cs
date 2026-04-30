using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Models.Responses;

/// <summary>
/// Represents a response that shows absences. 
/// </summary>
public class QueryableAbsenceResponse
{
    public int Count { get; set; }

    public List<AbsenceDto> Absences { get; set; } = new();
}
