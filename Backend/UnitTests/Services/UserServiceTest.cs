using AutoMapper;
using Core;
using Data;
using Data.Entities;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Schichtpilot.Exceptions;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Enums;
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
        await using var dbContext = CreateDbContext();
        var userDto = CreateUserDto();
        var mappedUser = CreateUser();

        mapperMock
            .Setup(mapper => mapper.Map<UserDto, User>(userDto))
            .Returns(mappedUser);

        userManagerMock
            .Setup(manager => manager.FindByEmailAsync(mappedUser.Email!))
            .ReturnsAsync(mappedUser);

        var service = new UserService(userManagerMock.Object, mapperMock.Object, loggerMock.Object, dbContext);

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
        await using var dbContext = CreateDbContext();
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

        var service = new UserService(userManagerMock.Object, mapperMock.Object, loggerMock.Object, dbContext);

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
        await using var dbContext = CreateDbContext();
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

        var service = new UserService(userManagerMock.Object, mapperMock.Object, loggerMock.Object, dbContext);

        await Assert.ThrowsAsync<AccountCreationException>(
            () => service.CreateUserAsync(userDto, "password"));
    }

    [Fact]
    public async Task CreateUserAsync_Success_CreatesUserAndAddsRole()
    {
        var userManagerMock = CreateUserManagerMock();
        var mapperMock = new Mock<IMapper>();
        var loggerMock = new Mock<ILogger<UserService>>();
        await using var dbContext = CreateDbContext();
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

        var service = new UserService(userManagerMock.Object, mapperMock.Object, loggerMock.Object, dbContext);

        await service.CreateUserAsync(userDto, "password");

        userManagerMock.Verify(manager => manager.CreateAsync(mappedUser, "password"), Times.Once);
        userManagerMock.Verify(manager => manager.AddToRoleAsync(mappedUser, UserRolesClass.User), Times.Once);
    }

    [Fact]
    public async Task GetUserDataAsync_UserNotFound_ThrowsUserNotFoundException()
    {
        var userManagerMock = CreateUserManagerMock();
        var mapperMock = new Mock<IMapper>();
        var loggerMock = new Mock<ILogger<UserService>>();
        await using var dbContext = CreateDbContext();

        var service = new UserService(userManagerMock.Object, mapperMock.Object, loggerMock.Object, dbContext);

        await Assert.ThrowsAsync<UserNotFoundException>(() => service.GetUserDataAsync(999));
    }

    [Fact]
    public async Task GetUserDataAsync_UserFound_ReturnsMappedUserDto()
    {
        var userManagerMock = CreateUserManagerMock();
        var mapperMock = new Mock<IMapper>();
        var loggerMock = new Mock<ILogger<UserService>>();
        await using var dbContext = CreateDbContext();
        var user = CreateUserWithId(42);
        var expectedDto = CreateUserDto();

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        mapperMock
            .Setup(mapper => mapper.Map<User, UserDto>(user))
            .Returns(expectedDto);

        var service = new UserService(userManagerMock.Object, mapperMock.Object, loggerMock.Object, dbContext);

        var result = await service.GetUserDataAsync(42);

        Assert.NotNull(result);
        Assert.Equal(expectedDto.Email, result.Email);
        Assert.Equal(expectedDto.FirstName, result.FirstName);
        Assert.Equal(expectedDto.LastName, result.LastName);
        Assert.Equal(expectedDto.Birthdate, result.Birthdate);
        Assert.Equal(expectedDto.AddressDto.City, result.AddressDto.City);
    }

    [Fact]
    public async Task GetUsersAsync_SortByLastNameAscending_ReturnsOrderedUsers()
    {
        var userManagerMock = CreateUserManagerMock();
        var mapperMock = new Mock<IMapper>();
        var loggerMock = new Mock<ILogger<UserService>>();
        await using var dbContext = CreateDbContext();

        mapperMock
            .Setup(mapper => mapper.Map<User, UserDto>(It.IsAny<User>()))
            .Returns((User user) => new UserDto
            {
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Birthdate = user.BirthDate,
                AddressDto = new AddressDto
                {
                    Street = user.StreetAddress,
                    City = user.City,
                    PostalCode = user.PostalCode
                }
            });

        var userA = CreateUserWithId(1);
        userA.FirstName = "Alice";
        userA.LastName = "Adams";

        var userB = CreateUserWithId(2);
        userB.FirstName = "Zoe";
        userB.LastName = "Zimmer";

        dbContext.Users.AddRange(userB, userA);
        await dbContext.SaveChangesAsync();

        var service = new UserService(userManagerMock.Object, mapperMock.Object, loggerMock.Object, dbContext);

        var result = await service.GetUsersAsync(
            new PaginationDto { Page = 1, PageSize = 10 },
            new UserSortingDto { SortProperty = UserSortEnum.LastName, Ascending = true },
            null);

        var users = result.Users.ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { "Adams", "Zimmer" }, users.Select(u => u.LastName).ToArray());
    }

    [Fact]
    public async Task GetUsersAsync_FilterByJobRoleStatusAndSearch_ReturnsMatchingUsers()
    {
        var userManagerMock = CreateUserManagerMock();
        var mapperMock = new Mock<IMapper>();
        var loggerMock = new Mock<ILogger<UserService>>();
        await using var dbContext = CreateDbContext();

        mapperMock
            .Setup(mapper => mapper.Map<User, UserDto>(It.IsAny<User>()))
            .Returns((User user) => new UserDto
            {
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Birthdate = user.BirthDate,
                AddressDto = new AddressDto
                {
                    Street = user.StreetAddress,
                    City = user.City,
                    PostalCode = user.PostalCode
                }
            });

        var nurseRole = new JobRole
        {
            Id = 1,
            Name = "Nurse",
            Description = "Nursing role",
            CreatedOn = DateTime.UtcNow
        };

        var doctorRole = new JobRole
        {
            Id = 2,
            Name = "Doctor",
            Description = "Doctor role",
            CreatedOn = DateTime.UtcNow
        };

        var matchingUser = CreateUserWithId(10);
        matchingUser.FirstName = "Alice";
        matchingUser.LastName = "Miller";
        matchingUser.EmailConfirmed = true;
        matchingUser.LockoutEnd = null;
        matchingUser.JobRoles = new HashSet<UserJobRoles>
        {
            new UserJobRoles { User = matchingUser, UserId = (int)matchingUser.Id, JobRole = nurseRole, JobRoleId = nurseRole.Id }
        };

        var nonMatchingStatus = CreateUserWithId(11);
        nonMatchingStatus.FirstName = "Ali";
        nonMatchingStatus.LastName = "Stone";
        nonMatchingStatus.EmailConfirmed = false;
        nonMatchingStatus.JobRoles = new HashSet<UserJobRoles>
        {
            new UserJobRoles { User = nonMatchingStatus, UserId = (int)nonMatchingStatus.Id, JobRole = nurseRole, JobRoleId = nurseRole.Id }
        };

        var nonMatchingRole = CreateUserWithId(12);
        nonMatchingRole.FirstName = "Alice";
        nonMatchingRole.LastName = "Mason";
        nonMatchingRole.EmailConfirmed = true;
        nonMatchingRole.JobRoles = new HashSet<UserJobRoles>
        {
            new UserJobRoles { User = nonMatchingRole, UserId = (int)nonMatchingRole.Id, JobRole = doctorRole, JobRoleId = doctorRole.Id }
        };

        dbContext.Users.AddRange(matchingUser, nonMatchingStatus, nonMatchingRole);
        await dbContext.SaveChangesAsync();

        var service = new UserService(userManagerMock.Object, mapperMock.Object, loggerMock.Object, dbContext);

        var filter = new UserFilterDto
        {
            JobFilters = new[] { "nurse" },
            AccountStatus = AccountStatusEnum.EmailVerified,
            Searchstring = "ali"
        };

        var result = await service.GetUsersAsync(
            new PaginationDto { Page = 1, PageSize = 10 },
            new UserSortingDto { SortProperty = UserSortEnum.Id, Ascending = true },
            filter);

        var users = result.Users.ToList();

        Assert.Equal(1, result.Count);
        Assert.Single(users);
        Assert.Equal(matchingUser.Email, users[0].Email);
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

    private static SchichtpilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SchichtpilotDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SchichtpilotDbContext(options);
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
            LastName = "User",
            StreetAddress = "Main Street 1",
            City = "Testville",
            PostalCode = 12345,
            BirthDate = new DateTime(1990, 1, 1)
        };
    }

    private static User CreateUserWithId(long id)
    {
        return new User
        {
            Id = id,
            Email = $"user{id}@test.com",
            UserName = $"user{id}@test.com",
            FirstName = "Test",
            LastName = "User",
            StreetAddress = "Main Street 1",
            City = "Testville",
            PostalCode = 12345,
            BirthDate = new DateTime(1990, 1, 1)
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
