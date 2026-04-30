namespace Schichtpilot.Interfaces;

/// <summary>
/// Orchestrates authenticate and logout operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user.
    /// </summary>
    /// <param name="email"> The email of the user. </param>
    /// <param name="password"> The password of the user. </param>
    /// <returns> Returns a JWT token with user details. </returns>
    Task<string> AuthenticateAsync(string email, string password);

    /// <summary>
    /// Logs out an authenticated user.
    /// </summary>
    /// <returns></returns>
    Task LogoutAsync();
}