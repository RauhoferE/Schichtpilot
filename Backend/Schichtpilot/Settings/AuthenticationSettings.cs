namespace Schichtpilot.Settings;

public class AuthenticationSettings
{
    public string JwtKey { get; set; }

    public int TokenLifeTimeInMinutes { get; set; }
}