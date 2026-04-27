namespace Schichtpilot.Exceptions;

/// <summary>
/// Represents an error when the user tries to login.
/// </summary>
public class LoginException : Exception
{
    public LoginException()
    {

    }

    public LoginException(string message) : base(message)
    {
    }

    public LoginException(string message, Exception innerException) : base(message, innerException)
    { }
}