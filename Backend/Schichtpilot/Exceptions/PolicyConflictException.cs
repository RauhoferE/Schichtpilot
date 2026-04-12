namespace Schichtpilot.Exceptions;

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