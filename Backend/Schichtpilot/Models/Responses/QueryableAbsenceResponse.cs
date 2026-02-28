using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Models.Responses;

public class QueryableAbsenceResponse
{
    public int Count { get; set; }
///*    public IQueryable<AbsenceDto> Absences { get; set; } = null!;
    public List<AbsenceDto> Absences { get; set; } = new(); // List for pagination, not IQueryable
    // Remove IQueryable - service returns materialized List after ProjectTo.ToListAsync()
}
