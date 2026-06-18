using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Models.Responses;

public class QueryableManagerAbsenceResponse
{
    public int Count { get; init; }
    public IEnumerable<ManagerAbsenceDto> Absences { get; init; } = [];
}