using Schichtpilot.Interfaces;

namespace Schichtpilot.Middleware;

public class UserContextMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserService userService)
    {
        if (context.User.Identity?.IsAuthenticated != null && context.User.Identity.IsAuthenticated)
        {
            var userId = await userService.GetUserIdAsync(context.User);
            context.Items["UserId"] = userId;
        }

        await _next(context);
    }
}