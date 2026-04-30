using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Data;
using Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Schichtpilot.Exceptions;
using Schichtpilot.Interfaces;
using Schichtpilot.Settings;

namespace Schichtpilot.Services;

/// <summary>
/// Orchestrates authenticate and logout operations.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly SchichtpilotDbContext _dbContext;
    private readonly AuthenticationSettings _authenticationSettings;

    public AuthService(UserManager<User> userManager, SignInManager<User> signInManager,
        SchichtpilotDbContext dbContext, IOptions<AuthenticationSettings> authenticationSettings)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authenticationSettings =
            authenticationSettings.Value ?? throw new ArgumentNullException(nameof(authenticationSettings));
    }

    /// <summary>
    /// Authenticates a user.
    /// </summary>
    /// <param name="email"> The email of the user. </param>
    /// <param name="password"> The password of the user. </param>
    /// <returns> Returns a JWT token with user details. </returns>
    /// <exception cref="LoginException"> Thrown when the user can't be found or the password is incorrect. </exception>
    public async Task<string> AuthenticateAsync(string email, string password)
    {
        var user = await this._userManager.FindByEmailAsync(email);

        if (user == null)
        {
            throw new LoginException($"User with email: {email} not found ");
        }

        var result = await this._signInManager.PasswordSignInAsync(user, password, false, false);

        if (!result.Succeeded)
        {
            throw new LoginException($"Login failed for: {email}");
        }

        var roles = await this._userManager.GetRolesAsync(user);

        return await this.GenerateJwtTokenAsync(user, roles);
    }

    /// <summary>
    /// Logs out an authenticated user.
    /// </summary>
    /// <returns></returns>
    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    /// <summary>
    /// Generates a jwt token with all necessary claims.
    /// </summary>
    /// <param name="user"> The user entity. </param>
    /// <param name="roles"> The roles assigned to the user. </param>
    /// <returns> Returns a jwt token in base64. </returns>
    private Task<string> GenerateJwtTokenAsync(User user, IList<string> roles)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(this._authenticationSettings.JwtKey);

        var claims = new List<Claim>()
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(this._authenticationSettings.TokenLifeTimeInMinutes),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)

        };
        var jwtSecurityToken = tokenHandler.CreateJwtSecurityToken(tokenDescriptor);
        return Task.FromResult(
            tokenHandler.WriteToken(jwtSecurityToken));
    }
}