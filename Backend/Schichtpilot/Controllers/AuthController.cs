using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.Requests;

namespace Schichtpilot.Controllers;

[Controller]
[Route("api/[controller]")]
public class AuthController : Controller
{
    private readonly IAuthService authService;
    
    private readonly IHostEnvironment _env;

    public AuthController(IAuthService authService, IHostEnvironment env)
    {
        this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
        this._env = env ?? throw new ArgumentNullException(nameof(env));
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Authenticate([FromBody, Required] LoginRequest loginRequest)
    {
        var jwtUserToken = await this.authService.AuthenticateAsync(loginRequest.Email, loginRequest.Password);
        this.AttachCookie(jwtUserToken);
        return this.Ok();
    }

    [HttpGet("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await this.authService.LogoutAsync();
        return this.NoContent();
    }
    
    private void AttachCookie(string jwtUserToken)
    {
        HttpContext.Response.Cookies.Append("SchichtpilotUser", jwtUserToken, new CookieOptions()
        {
            HttpOnly = false,
            Secure = true,
            SameSite = this._env.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Strict,
        });
    }
}