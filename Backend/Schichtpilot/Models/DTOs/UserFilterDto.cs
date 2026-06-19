using Schichtpilot.Models.Enums;

namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Used to filter users.
/// </summary>
public class UserFilterDto
{
    public string[] JobFilters { get; init; } = [];
    public AccountStatusEnum AccountStatus { get; init; }
    public string? Searchstring { get; init; }
}