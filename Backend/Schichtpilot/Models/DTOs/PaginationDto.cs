namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents a request for pagination.
/// </summary>
public class PaginationDto
{
    public int Page { get; init; }
    public int PageSize { get; init; }
}