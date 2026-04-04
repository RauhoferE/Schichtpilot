using Data.Entities;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Models.Responses;

public class QueryableUserResponse
{
    public int Count { get; set; }
    public IEnumerable<UserDto> Users { get; set; }
}