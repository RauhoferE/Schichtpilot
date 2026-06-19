namespace Schichtpilot.Settings;

/// <summary>
/// Contains the settings for the jwt cookie created for authentication purposes.
/// </summary>
public class AuthenticationSettings
{
    public required string JwtKey { get; set; }

    public int TokenLifeTimeInMinutes { get; set; }
}