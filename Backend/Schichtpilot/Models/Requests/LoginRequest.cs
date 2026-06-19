namespace Schichtpilot.Models.Requests;

/// <summary>
/// Represents a request to login a user.
/// </summary>
public class LoginRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}