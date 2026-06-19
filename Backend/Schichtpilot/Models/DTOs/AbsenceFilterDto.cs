using Schichtpilot.Models.Enums;

namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Used to filter absences.
/// </summary>
public class AbsenceFilterDto
{
    public List<AbsenceStatusEnum>? Status { get; init; }
    public List<AbsenceTypeEnum>? AbsenceType { get; init; }
    public DateTime? CreatedFrom { get; init; }
    public DateTime? CreatedTo { get; init; }
    public DateTime? StartDateFrom { get; init; }
    public DateTime? StartDateTo { get; init; }
    public string? Searchstring { get; init; } // employee/type
}
