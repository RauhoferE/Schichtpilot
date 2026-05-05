namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a request to update an absence.
/// </summary>
public class StatusUpdateDto
{
    public required string Status { get; set; } //Pending, Approved, Denied
    public string? ManagerMessage { get; set; }        // optional for Approved, Required for Denied (validated in service)
}