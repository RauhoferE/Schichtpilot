using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using Schichtpilot.Interfaces;
using Schichtpilot.Middleware;
using Xunit;

namespace UnitTests.Middleware;

public class UserContextMiddlewareTest
{
    [Fact]
    public async Task InvokeAsync_AuthenticatedUser_SetsUserIdAndCallsNext()
    {
        var userServiceMock = new Mock<IUserService>();
        userServiceMock
            .Setup(service => service.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(42);

        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "user@test.com") },
            "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new UserContextMiddleware(next);

        await middleware.InvokeAsync(context, userServiceMock.Object);

        Assert.True(nextCalled);
        Assert.True(context.Items.ContainsKey("UserId"));
        Assert.Equal(42L, context.Items["UserId"]);
        userServiceMock.Verify(service => service.GetUserIdAsync(context.User), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_UnauthenticatedUser_DoesNotSetUserIdAndCallsNext()
    {
        var userServiceMock = new Mock<IUserService>();

        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity());

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new UserContextMiddleware(next);

        await middleware.InvokeAsync(context, userServiceMock.Object);

        Assert.True(nextCalled);
        Assert.False(context.Items.ContainsKey("UserId"));
        userServiceMock.Verify(service => service.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
    }
}
