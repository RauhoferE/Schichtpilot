using Microsoft.Identity.Client;

namespace Schichtpilot.Interfaces;

public interface IAuthService
{
    Task<AuthenticationResult> Authenticate(string email, string password);
}