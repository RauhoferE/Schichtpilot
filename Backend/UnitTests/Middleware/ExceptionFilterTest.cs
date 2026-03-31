using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Schichtpilot.Exceptions;
using Schichtpilot.Middleware;

namespace UnitTests.Middleware;

public class ExceptionFilterTest
{
    public static IEnumerable<object[]> ExceptionCases()
    {
        yield return new object[] { new Exception("Generic error"), StatusCodes.Status400BadRequest, "Generic error" };
        yield return new object[] { new AlreadyExistsException("Exists"), StatusCodes.Status409Conflict, "Exists" };
        yield return new object[] { new NotFoundException("Missing"), StatusCodes.Status404NotFound, "Missing" };
        yield return new object[] { new LoginException("Bad password"), StatusCodes.Status400BadRequest, "Login Failed" };
        yield return new object[] { new NotSetException("Not set"), StatusCodes.Status406NotAcceptable, "Not set" };
        yield return new object[] { new AccountCreationException("Create failed"), StatusCodes.Status500InternalServerError, "Registration failed" };
        yield return new object[] { new UserNotFoundException("User missing"), StatusCodes.Status404NotFound, "User missing" };
        yield return new object[] { new InvalidDependencyException("Dependency invalid"), StatusCodes.Status409Conflict, "Dependency invalid" };
        yield return new object[] { new PolicyConflictException("Policy conflict"), StatusCodes.Status424FailedDependency, "Policy conflict" };
    }

    [Theory]
    [MemberData(nameof(ExceptionCases))]
    public void OnException_SetsExpectedStatusCodeAndMessage(Exception exception, int expectedStatusCode, string expectedMessage)
    {
        var logger = new Mock<ILogger<ExceptionFilter>>();
        var filter = new ExceptionFilter(logger.Object);

        var context = CreateExceptionContext(exception);

        filter.OnException(context);

        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        var result = Assert.IsType<ContentResult>(context.Result);
        Assert.False(string.IsNullOrWhiteSpace(result.Content));

        using var document = JsonDocument.Parse(result.Content!);
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("statusCode", out var statusElement));
        Assert.Equal(expectedStatusCode, statusElement.GetInt32());

        Assert.True(root.TryGetProperty("message", out var messageElement));
        Assert.Equal(expectedMessage, messageElement.GetString());
    }

    private static ExceptionContext CreateExceptionContext(Exception exception)
    {
        var httpContext = new DefaultHttpContext();
        var routeData = new RouteData();
        var actionDescriptor = new ActionDescriptor();
        var modelState = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();

        var actionContext = new ActionContext(httpContext, routeData, actionDescriptor, modelState);
        var filters = new List<IFilterMetadata>();

        var context = new ExceptionContext(actionContext, filters)
        {
            Exception = exception
        };

        return context;
    }
}
