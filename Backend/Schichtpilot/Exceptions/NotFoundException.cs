namespace Schichtpilot.Exceptions;

/// <summary>
/// Represents an error when an entity cannot be found.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException()
    {

    }

    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    { }
}