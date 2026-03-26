using Microsoft.AspNetCore.Mvc.Filters;
using Schichtpilot.Exceptions;

namespace Schichtpilot.Middleware;

public class ExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ExceptionFilter> _logger;

    public ExceptionFilter(ILogger<ExceptionFilter> logger)
    {
        _logger = logger;
    }

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
    }
}