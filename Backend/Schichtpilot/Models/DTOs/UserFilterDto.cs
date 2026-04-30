using Schichtpilot.Models.Enums;

namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Used to filter users.
/// </summary>
public class UserFilterDto
{
    public string[] JobFilters { get; set; }
    public AccountStatusEnum AccountStatus { get; set; }
    public string? Searchstring { get; set; }
}