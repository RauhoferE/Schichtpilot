namespace Schichtpilot.Exceptions;

public class InvalidDependencyException : Exception
{
    public InvalidDependencyException()
    {
        
    }
    
    public InvalidDependencyException(string message):base(message)
    {
    }
    
    public InvalidDependencyException(string message, Exception innerException) : base(message, innerException)
    {}
}