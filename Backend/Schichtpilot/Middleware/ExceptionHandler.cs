using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Schichtpilot.Exceptions;

namespace Schichtpilot.Middleware;

/// <summary>
/// Used to catch exceptions thrown by the application.
/// </summary>
public class ExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ExceptionFilter> _logger;

    public ExceptionFilter(ILogger<ExceptionFilter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns various status codes and messages depending on the thrown exception.
    /// </summary>
    /// <param name="context"> The context in which the exception is thrown. </param>
    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        this._logger.LogError(exception, exception.Message);

        var statusCode = StatusCodes.Status400BadRequest;

        var responseMessage = exception.Message;

        if (exception.GetType() == typeof(AlreadyExistsException))
        {
            statusCode = StatusCodes.Status409Conflict;
        }

        if (exception.GetType() == typeof(NotFoundException))
        {
            statusCode = StatusCodes.Status404NotFound;
        }

        if (exception.GetType() == typeof(LoginException))
        {
            // Dont give the user the reason for the login failure for security reasons
            responseMessage = "Login Failed";
        }

        if (exception.GetType() == typeof(NotSetException))
        {
            statusCode = StatusCodes.Status406NotAcceptable;
        }

        if (exception.GetType() == typeof(AccountCreationException))
        {
            statusCode = StatusCodes.Status500InternalServerError;
            responseMessage = "Registration failed";
        }

        if (exception.GetType() == typeof(UserNotFoundException))
        {
            statusCode = StatusCodes.Status404NotFound;
        }

        if (exception.GetType() == typeof(InvalidDependencyException))
        {
            statusCode = StatusCodes.Status409Conflict;
        }

        if (exception.GetType() == typeof(PolicyConflictException))
        {
            statusCode = StatusCodes.Status424FailedDependency;
        }

        context.Result = new ContentResult
        {
            Content = JsonConvert.SerializeObject(
                new
                {
                    statusCode,
                    message = responseMessage,
#if DEBUG
                    stackTrace = exception.StackTrace,
                    exceptionType = exception.GetType().Name
#endif
                }),
            StatusCode = statusCode,
            ContentType = "application/json"
            

        };
    }
}