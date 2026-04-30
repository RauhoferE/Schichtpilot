using Schichtpilot.Models.Enums;

namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Used to filter absences.
/// </summary>
public class AbsenceFilterDto
{
    public List<AbsenceStatusEnum>? Status { get; set; }
    public List<AbsenceTypeEnum>? AbsenceType { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public string? Searchstring { get; set; } // employee/type
}
