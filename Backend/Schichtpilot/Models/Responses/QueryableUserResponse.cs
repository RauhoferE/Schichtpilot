using Data.Entities;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Models.Responses;

/// <summary>
/// Represents a response that shows all users. 
/// </summary>
public class QueryableUserResponse
{
    public int Count { get; set; }
    public IEnumerable<UserDto> Users { get; set; }
}