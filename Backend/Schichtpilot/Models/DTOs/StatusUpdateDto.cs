namespace Schichtpilot.Models.DTOs;

public class StatusUpdateDto
{
    public required string Status { get; set; } // Approved, Denied
    public string? Message { get; set; }        // Required for Denied (validated in service)
}
