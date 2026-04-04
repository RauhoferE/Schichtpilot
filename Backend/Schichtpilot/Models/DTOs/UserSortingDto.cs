using Schichtpilot.Models.Enums;

namespace Schichtpilot.Models.DTOs;

public class UserSortingDto
{
    public UserSortEnum SortProperty { get; set; }
    public bool Ascending { get; set; }
}