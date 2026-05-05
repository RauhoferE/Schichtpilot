using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Models.Responses;

/// <summary>
/// Contains errors from validating models.
/// </summary>
public class ErrorResponse
{
    public List<ErrorStateDto> ErrorStates { get; set; } = new List<ErrorStateDto>();
}