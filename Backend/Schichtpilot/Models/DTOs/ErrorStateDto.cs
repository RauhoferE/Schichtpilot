namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents an error when validating a model.
/// </summary>
public class ErrorStateDto
{
    public string? FieldName { get; set; }

    public string? Message { get; set; }
}