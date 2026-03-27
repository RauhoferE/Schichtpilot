using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Models.Responses;

public class ErrorResponse
{
    public List<ErrorStateDto> ErrorStates { get; set; } = new List<ErrorStateDto>();
}