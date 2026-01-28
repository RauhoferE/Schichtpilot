using Microsoft.Identity.Client;
using Schichtpilot.Interfaces;

namespace Schichtpilot.Services;

public class AuthService : IAuthService
{
    public Task<AuthenticationResult> Authenticate(string email, string password)
    {
        throw new NotImplementedException();
    }
}