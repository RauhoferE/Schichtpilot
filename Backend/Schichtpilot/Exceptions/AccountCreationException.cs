namespace Schichtpilot.Exceptions;

public class AccountCreationException : Exception
{
    public AccountCreationException()
    {
        
    }
    
    public AccountCreationException(string message):base(message)
    {
    }
    
    public AccountCreationException(string message, Exception innerException) : base(message, innerException)
    {}
}