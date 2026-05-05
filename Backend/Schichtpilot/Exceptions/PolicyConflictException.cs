namespace Schichtpilot.Exceptions;

/// <summary>
/// Represents an error that occurs when a constraint is broken.
/// </summary>
public class PolicyConflictException : Exception
{
    public PolicyConflictException()
    {

    }

    public PolicyConflictException(string message) : base(message)
    {
    }

    public PolicyConflictException(string message, Exception innerException) : base(message, innerException)
    { }
}