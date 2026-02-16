namespace Schichtpilot.Models.DTOs;

public class AbsenceFilterDto
{
    public DateTime? StartFrom { get; set; }
    public string? EmployeeEmail { get; set; }
    public string? Reason { get; set; }
    public string? Status { get; set; }
    
    // Excel column sorting ↑↓
    public string? SortBy { get; set; } = "StartDate";     // EmployeeName, StartDate, EndDate, Reason, Status
    public string? SortDir { get; set; } = "desc";         // asc, desc
}
