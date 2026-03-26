namespace Schichtpilot.Models.Responses;

public class ErrorResponse
{
    public IEnumerable<string> Errors { get; set; } = new List<string>();
}