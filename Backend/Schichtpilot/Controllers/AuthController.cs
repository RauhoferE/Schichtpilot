using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.Requests;

namespace Schichtpilot.Controllers;

/// <summary>
/// Provides an endpoint to login and logout.
/// </summary>
[Controller]
[Route("api/[controller]")]
public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly IHostEnvironment _env;

    public AuthController(
        IAuthService authService,
        IHostEnvironment env)
    {
        this._authService = authService ?? throw new ArgumentNullException(nameof(authService));
        this._env = env ?? throw new ArgumentNullException(nameof(env));
    }

    /// <summary>
    /// Authenticates the user.
    /// </summary>
    /// <param name="loginRequest"> The details of the user. </param>
    /// <returns> Returns an Ok response and the authentication cookie. </returns>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Authenticate([FromBody, Required] LoginRequest loginRequest)
    {
        var jwtUserToken = await this._authService.AuthenticateAsync(loginRequest.Email, loginRequest.Password);
        this.AttachCookie(jwtUserToken);
        return this.Ok();
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    /// <returns> Returns a no content response. </returns>
    [HttpGet("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await this._authService.LogoutAsync();

        // HttpContext.Response.Cookies.Delete("SchichtpilotUser", new CookieOptions()
        // {
        //     HttpOnly = true,
        //     Secure = this._env.IsDevelopment() ? false : true,
        //     SameSite = this._env.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Strict,
        //     Path = "/"
        // });

        return this.NoContent();
    }

    /// <summary>
    /// Attaches the provided jwt token as a cookie.
    /// </summary>
    /// <param name="jwtUserToken"> The jwt token with all user details. </param>
    private void AttachCookie(string jwtUserToken)
    {
        HttpContext.Response.Cookies.Append("SchichtpilotUser", jwtUserToken, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = this._env.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Strict,
            Path = "/"
        });
    }
}