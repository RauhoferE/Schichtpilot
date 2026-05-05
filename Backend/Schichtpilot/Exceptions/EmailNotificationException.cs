namespace Schichtpilot.Exceptions;

/// <summary>
/// Represents an error when an email could not be sent.
/// </summary>
public class EmailNotificationException : Exception
{
    public EmailNotificationException(string message) : base(message) { }
    public EmailNotificationException(string message, Exception inner) : base(message, inner) { }
}