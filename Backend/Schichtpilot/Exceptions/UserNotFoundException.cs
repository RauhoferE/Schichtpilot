namespace Schichtpilot.Exceptions;

/// <summary>
/// Represents an error when a user could not be found.
/// </summary>
public class UserNotFoundException : Exception
{
    public UserNotFoundException()
    {

    }

    public UserNotFoundException(string message) : base(message)
    {
    }

    public UserNotFoundException(string message, Exception innerException) : base(message, innerException)
    { }
}
