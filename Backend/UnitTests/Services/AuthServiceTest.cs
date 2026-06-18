using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Data;
using Data.Entities;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Schichtpilot.Exceptions;
using Schichtpilot.Services;
using Schichtpilot.Settings;

namespace UnitTests.Services;

[TestSubject(typeof(AuthService))]
public class AuthServiceTest
{
    [Fact]
    public async Task AuthenticateAsync_UserNotFound_ThrowsLoginException()
    {
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock.Object);
        var dbContext = CreateDbContext();
        var settings = CreateAuthSettings();

        userManagerMock
            .Setup(manager => manager.FindByEmailAsync("missing@test.com"))
            .ReturnsAsync((User?)null);

        var service = new AuthService(
            userManagerMock.Object,
            signInManagerMock.Object,
            dbContext,
            Options.Create(settings));

        await Assert.ThrowsAsync<LoginException>(
            () => service.AuthenticateAsync("missing@test.com", "password"));
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidPassword_ThrowsLoginException()
    {
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock.Object);
        var dbContext = CreateDbContext();
        var settings = CreateAuthSettings();
        var user = CreateUser();

        userManagerMock
            .Setup(manager => manager.FindByEmailAsync(user.Email!))
            .ReturnsAsync(user);

        signInManagerMock
            .Setup(manager => manager.PasswordSignInAsync(user, "wrong", false, false))
            .ReturnsAsync(SignInResult.Failed);

        var service = new AuthService(
            userManagerMock.Object,
            signInManagerMock.Object,
            dbContext,
            Options.Create(settings));

        await Assert.ThrowsAsync<LoginException>(
            () => service.AuthenticateAsync(user.Email!, "wrong"));
    }

    [Fact]
    public async Task AuthenticateAsync_Success_ReturnsJwtWithExpectedClaimsAndRoles()
    {
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock.Object);
        var dbContext = CreateDbContext();
        var settings = CreateAuthSettings();
        var user = CreateUser();
        var roles = new List<string> { "Admin", "User" };

        userManagerMock
            .Setup(manager => manager.FindByEmailAsync(user.Email!))
            .ReturnsAsync(user);

        signInManagerMock
            .Setup(manager => manager.PasswordSignInAsync(user, "secret", false, false))
            .ReturnsAsync(SignInResult.Success);

        userManagerMock
            .Setup(manager => manager.GetRolesAsync(user))
            .ReturnsAsync(roles);

        var service = new AuthService(
            userManagerMock.Object,
            signInManagerMock.Object,
            dbContext,
            Options.Create(settings));

        var token = await service.AuthenticateAsync(user.Email!, "secret");

        Assert.False(string.IsNullOrWhiteSpace(token));

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Contains(jwt.Claims, claim => claim.Type == JwtRegisteredClaimNames.Email && claim.Value == user.Email);
        Assert.Contains(jwt.Claims, claim => claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == user.Id.ToString());
        Assert.Contains(jwt.Claims, claim => claim.Type == JwtRegisteredClaimNames.Name && claim.Value == user.FirstName);
        Assert.Contains(jwt.Claims, claim => claim.Type == JwtRegisteredClaimNames.FamilyName && claim.Value == user.LastName);

        var roleClaims = jwt.Claims.Where(claim => claim.Type == "role").Select(claim => claim.Value).ToList();
        Assert.Contains("Admin", roleClaims);
        Assert.Contains("User", roleClaims);

        var now = DateTime.UtcNow;
        Assert.True(jwt.ValidTo >= now.AddMinutes(settings.TokenLifeTimeInMinutes - 1));
        Assert.True(jwt.ValidTo <= now.AddMinutes(settings.TokenLifeTimeInMinutes + 1));
    }

    [Fact]
    public async Task LogoutAsync_CallsSignOut()
    {
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock.Object);
        var dbContext = CreateDbContext();
        var settings = CreateAuthSettings();

        var service = new AuthService(
            userManagerMock.Object,
            signInManagerMock.Object,
            dbContext,
            Options.Create(settings));

        await service.LogoutAsync();

        signInManagerMock.Verify(manager => manager.SignOutAsync(), Times.Once);
    }

    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            store.Object,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<User>>>().Object);
    }

    private static Mock<SignInManager<User>> CreateSignInManagerMock(UserManager<User> userManager)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        contextAccessor.Setup(accessor => accessor.HttpContext).Returns(new DefaultHttpContext());

        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
        var options = Options.Create(new IdentityOptions());
        var logger = new Mock<ILogger<SignInManager<User>>>();
        var schemes = new Mock<IAuthenticationSchemeProvider>();
        var confirmation = new Mock<IUserConfirmation<User>>();

        return new Mock<SignInManager<User>>(
            userManager,
            contextAccessor.Object,
            claimsFactory.Object,
            options,
            logger.Object,
            schemes.Object,
            confirmation.Object);
    }

    private static SchichtpilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SchichtpilotDbContext>().Options;
        return new SchichtpilotDbContext(options);
    }

    private static AuthenticationSettings CreateAuthSettings()
    {
        return new AuthenticationSettings
        {
            JwtKey = "super-secret-key-for-tests-only-123456789",
            TokenLifeTimeInMinutes = 60
        };
    }

    private static User CreateUser()
    {
        return new User
        {
            Email = "user@test.com",
            UserName = "user@test.com",
            FirstName = "Test",
            LastName = "User"
        };
    }
}
