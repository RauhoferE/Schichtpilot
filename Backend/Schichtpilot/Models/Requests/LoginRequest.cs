namespace Schichtpilot.Models.Requests;

/// <summary>
/// Represents a request to login a user.
/// </summary>
public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}