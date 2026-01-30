using AutoMapper;
using Core;
using Data.Entities;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Schichtpilot.Exceptions;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Services;

namespace UnitTests.Services;

[TestSubject(typeof(UserService))]
public class UserServiceTest
{
    [Fact]
    public async Task CreateUserAsync_UserAlreadyExists_LogsWarningAndReturns()
    {
        var userManagerMock = CreateUserManagerMock();
        var mapperMock = new Mock<IMapper>();
        var loggerMock = new Mock<ILogger<UserService>>();
        var userDto = CreateUserDto();
        var mappedUser = CreateUser();

        mapperMock
            .Setup(mapper => mapper.Map<UserDto, User>(userDto))
            .Returns(mappedUser);

        userManagerMock
            .Setup(manager => manager.FindByEmailAsync(mappedUser.Email!))
            .ReturnsAsync(mappedUser);

        var service = new UserService(userManagerMock.Object, mapperMock.Object, loggerMock.Object);

        await service.CreateUserAsync(userDto, "password");

        userManagerMock.Verify(manager => manager.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        userManagerMock.Verify(manager => manager.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        VerifyLog(loggerMock, LogLevel.Warning, $"User already exists for: {mappedUser.Email}");
    }

    [Fact]
    public async Task CreateUserAsync_CreateFails_ThrowsAccountCreationException()
    {
        var userManagerMock = CreateUserManagerMock();
        var mapperMock = new Mock<IMapper>();
        var loggerMock = new Mock<ILogger<UserService>>();
        var userDto = CreateUserDto();
        var mappedUser = CreateUser();

        mapperMock
            .Setup(mapper => mapper.Map<UserDto, User>(userDto))
            .Returns(mappedUser);

        userManagerMock
            .Setup(manager => manager.FindByEmailAsync(mappedUser.Email!))
            .ReturnsAsync((User?)null);

        userManagerMock
            .Setup(manager => manager.CreateAsync(mappedUser, "password"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "create failed" }));

        var service = new UserService(userManagerMock.Object, mapperMock.Object, loggerMock.Object);

        await Assert.ThrowsAsync<AccountCreationException>(
            () => service.CreateUserAsync(userDto, "password"));

        userManagerMock.Verify(manager => manager.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_AddToRoleFails_ThrowsAccountCreationException()
    {
        var userManagerMock = CreateUserManagerMock();
        var mapperMock = new Mock<IMapper>();
        var loggerMock = new Mock<ILogger<UserService>>();
        var userDto = CreateUserDto();
        var mappedUser = CreateUser();

        mapperMock
            .Setup(mapper => mapper.Map<UserDto, User>(userDto))
            .Returns(mappedUser);

        userManagerMock
            .Setup(manager => manager.FindByEmailAsync(mappedUser.Email!))
            .ReturnsAsync((User?)null);

        userManagerMock
            .Setup(manager => manager.CreateAsync(mappedUser, "password"))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock
            .Setup(manager => manager.AddToRoleAsync(mappedUser, UserRolesClass.User))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "role failed" }));

        var service = new UserService(userManagerMock.Object, mapperMock.Object, loggerMock.Object);

        await Assert.ThrowsAsync<AccountCreationException>(
            () => service.CreateUserAsync(userDto, "password"));
    }

    [Fact]
    public async Task CreateUserAsync_Success_CreatesUserAndAddsRole()
    {
        var userManagerMock = CreateUserManagerMock();
        var mapperMock = new Mock<IMapper>();
        var loggerMock = new Mock<ILogger<UserService>>();
        var userDto = CreateUserDto();
        var mappedUser = CreateUser();

        mapperMock
            .Setup(mapper => mapper.Map<UserDto, User>(userDto))
            .Returns(mappedUser);

        userManagerMock
            .Setup(manager => manager.FindByEmailAsync(mappedUser.Email!))
            .ReturnsAsync((User?)null);

        userManagerMock
            .Setup(manager => manager.CreateAsync(mappedUser, "password"))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock
            .Setup(manager => manager.AddToRoleAsync(mappedUser, UserRolesClass.User))
            .ReturnsAsync(IdentityResult.Success);

        var service = new UserService(userManagerMock.Object, mapperMock.Object, loggerMock.Object);

        await service.CreateUserAsync(userDto, "password");

        userManagerMock.Verify(manager => manager.CreateAsync(mappedUser, "password"), Times.Once);
        userManagerMock.Verify(manager => manager.AddToRoleAsync(mappedUser, UserRolesClass.User), Times.Once);
    }

    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            store.Object,
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<IPasswordHasher<User>>().Object,
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            new Mock<ILookupNormalizer>().Object,
            new IdentityErrorDescriber(),
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<User>>>().Object);
    }

    private static UserDto CreateUserDto()
    {
        return new UserDto
        {
            Email = "user@test.com",
            FirstName = "Test",
            LastName = "User",
            Birthdate = new DateTime(1990, 1, 1),
            AddressDto = new AddressDto
            {
                Street = "Main Street 1",
                City = "Testville",
                PostalCode = 12345
            }
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

    private static void VerifyLog(
        Mock<ILogger<UserService>> loggerMock,
        LogLevel level,
        string message)
    {
        loggerMock.Verify(
            logger => logger.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString() == message),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
