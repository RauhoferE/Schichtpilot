namespace Schichtpilot.Exceptions;

/// <summary>
/// Represents an error that occurs when an account is created.
/// </summary>
public class AccountCreationException : Exception
{
    public AccountCreationException()
    {

    }

    public AccountCreationException(string message) : base(message)
    {
    }

    public AccountCreationException(string message, Exception innerException) : base(message, innerException)
    { }
}