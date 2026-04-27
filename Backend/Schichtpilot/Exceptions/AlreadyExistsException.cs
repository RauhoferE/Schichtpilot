namespace Schichtpilot.Exceptions;

/// <summary>
/// Represents an error when an entity already exists.
/// </summary>
public class AlreadyExistsException : Exception
{
    public AlreadyExistsException()
    {

    }

    public AlreadyExistsException(string message) : base(message)
    {
    }

    public AlreadyExistsException(string message, Exception innerException) : base(message, innerException)
    { }
}