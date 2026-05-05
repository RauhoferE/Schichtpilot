using Schichtpilot.Interfaces;

namespace IntegrationTests.Fakes;

public class FakeAuthService : IAuthService
{
    public string? LastEmail { get; private set; }
    public string? LastPassword { get; private set; }
    public int AuthenticateCallCount { get; private set; }
    public int LogoutCallCount { get; private set; }

    public Task<string> AuthenticateAsync(string email, string password)
    {
        AuthenticateCallCount++;
        LastEmail = email;
        LastPassword = password;
        return Task.FromResult("fake-jwt-token");
    }

    public Task LogoutAsync()
    {
        LogoutCallCount++;
        return Task.CompletedTask;
    }

    public void Reset()
    {
        LastEmail = null;
        LastPassword = null;
        AuthenticateCallCount = 0;
        LogoutCallCount = 0;
    }
}
