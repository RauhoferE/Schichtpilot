using Schichtpilot.Models.Enums;

namespace Schichtpilot.Models.DTOs;

public class UserFilterDto
{
    public string[] JobFilters { get; set; }
    public AccountStatusEnum AccountStatus { get; set; }
    public string? Searchstring { get; set; }
}