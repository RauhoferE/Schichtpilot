using Schichtpilot.Interfaces;

namespace Schichtpilot.Middleware;

/// <summary>
/// Used to get the user id from the jwt cookie. 
/// </summary>
public class UserContextMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Assigns the user cookie to a context item.
    /// </summary>
    /// <param name="context"> The current http context. </param>
    /// <param name="userService"> The user service that is able to access the registered users. </param>
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