namespace Schichtpilot.Interfaces;

public interface IAuthService
{
    Task<string> AuthenticateAsync(string email, string password);

    Task LogoutAsync();
}