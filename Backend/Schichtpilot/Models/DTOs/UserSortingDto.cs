using Schichtpilot.Models.Enums;

namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Used to sort users.
/// </summary>
public class UserSortingDto
{
    public UserSortEnum SortProperty { get; set; }
    public bool Ascending { get; set; }
}