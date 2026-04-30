namespace Schichtpilot.Exceptions;

/// <summary>
/// Represents an error when the company policy is not set.
/// </summary>
public class NotSetException : Exception
{
    public NotSetException()
    {

    }

    public NotSetException(string message) : base(message)
    {
    }

    public NotSetException(string message, Exception innerException) : base(message, innerException)
    { }
}