using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Models.Responses;

public class QueryableManagerAbsenceResponse
{
    public int Count { get; set; }
    public IEnumerable<ManagerAbsenceDto> Absences { get; set; } = [];
}