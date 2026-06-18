using Schichtpilot.Models.Enums;

namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Used to sort users.
/// </summary>
public class UserSortingDto
{
    public UserSortEnum SortProperty { get; init; }
    public bool Ascending { get; init; }
}