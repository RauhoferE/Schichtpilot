using AutoMapper;
using Data;
using Data.Entities;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Moq;
using Schichtpilot.Exceptions;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Services;

namespace UnitTests.Services;

[TestSubject(typeof(JobRoleService))]
public class JobRoleServiceTest
{
    [Fact]
    public async Task CreateJobRoleAsync_AlreadyExists_ThrowsAlreadyExistsException()
    {
        await using var dbContext = CreateDbContext();
        dbContext.JobRoles.Add(CreateJobRole(1, "Nurse"));
        await dbContext.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = new JobRoleService(dbContext, mapperMock.Object);

        var dto = new CreateJobRoleDto
        {
            Name = "Nurse",
            Description = "Nursing role",
            DependentOnJobRoleIds = new List<int>()
        };

        await Assert.ThrowsAsync<AlreadyExistsException>(() => service.CreateJobRoleAsync(dto));
    }

    [Fact]
    public async Task CreateJobRoleAsync_Success_CreatesJobRoleAndDependencies()
    {
        await using var dbContext = CreateDbContext();
        var prerequisite1 = CreateJobRole(1, "Nurse");
        var prerequisite2 = CreateJobRole(2, "Doctor");
        dbContext.JobRoles.AddRange(prerequisite1, prerequisite2);
        await dbContext.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = new JobRoleService(dbContext, mapperMock.Object);

        var dto = new CreateJobRoleDto
        {
            Name = "Senior Nurse",
            Description = "Senior nursing role",
            DependentOnJobRoleIds = new List<int> { prerequisite1.Id, prerequisite2.Id }
        };

        await service.CreateJobRoleAsync(dto);

        var created = await dbContext.JobRoles.FirstOrDefaultAsync(x => x.Name == "Senior Nurse");
        Assert.NotNull(created);

        var dependencies = await dbContext.JobRoleDependencies
            .Include(x => x.JobRole)
            .Include(x => x.Dependency)
            .Where(x => x.JobRole != null && x.JobRole.Id == created.Id)
            .ToListAsync();

        Assert.Equal(2, dependencies.Count);
        Assert.Contains(dependencies, d => d.Dependency.Id == prerequisite1.Id);
        Assert.Contains(dependencies, d => d.Dependency.Id == prerequisite2.Id);
    }

    [Fact]
    public async Task UpdateJobRoleAsync_NameExists_ThrowsAlreadyExistsException()
    {
        await using var dbContext = CreateDbContext();
        dbContext.JobRoles.AddRange(
            CreateJobRole(1, "Nurse"),
            CreateJobRole(2, "Doctor"));
        await dbContext.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = new JobRoleService(dbContext, mapperMock.Object);

        var dto = new EditJobRoleDto
        {
            Name = "Doctor",
            Description = "Updated"
        };

        await Assert.ThrowsAsync<AlreadyExistsException>(() => service.UpdateJobRoleAsync(1, dto));
    }

    [Fact]
    public async Task UpdateJobRoleAsync_NotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = new Mock<IMapper>();
        var service = new JobRoleService(dbContext, mapperMock.Object);

        var dto = new EditJobRoleDto
        {
            Name = "Nurse",
            Description = "Updated"
        };

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateJobRoleAsync(999, dto));
    }

    [Fact]
    public async Task UpdateJobRoleAsync_Success_UpdatesFields()
    {
        await using var dbContext = CreateDbContext();
        var role = CreateJobRole(1, "Nurse");
        dbContext.JobRoles.Add(role);
        await dbContext.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = new JobRoleService(dbContext, mapperMock.Object);

        var dto = new EditJobRoleDto
        {
            Name = "Senior Nurse",
            Description = "Updated description"
        };

        await service.UpdateJobRoleAsync(role.Id, dto);

        var updated = await dbContext.JobRoles.FirstOrDefaultAsync(x => x.Id == role.Id);
        Assert.NotNull(updated);
        Assert.Equal("Senior Nurse", updated.Name);
        Assert.Equal("Updated description", updated.Description);
    }

    [Fact]
    public async Task AddDependenciesToJobRole_JobRoleNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        dbContext.JobRoles.Add(CreateJobRole(1, "Nurse"));
        await dbContext.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = new JobRoleService(dbContext, mapperMock.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => service.AddDependenciesToJobRole(999, 1));
    }

    [Fact]
    public async Task AddDependenciesToJobRole_DependencyNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        dbContext.JobRoles.Add(CreateJobRole(1, "Nurse"));
        await dbContext.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = new JobRoleService(dbContext, mapperMock.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => service.AddDependenciesToJobRole(1, 999));
    }

    [Fact]
    public async Task AddDependenciesToJobRole_DependencyExists_ThrowsAlreadyExistsException()
    {
        await using var dbContext = CreateDbContext();
        var jobRole = CreateJobRole(1, "Nurse");
        var dependency = CreateJobRole(2, "Doctor");
        dbContext.JobRoles.AddRange(jobRole, dependency);
        dbContext.JobRoleDependencies.Add(new JobRoleDependency
        {
            JobRoleId = jobRole.Id,
            DependencyJobRoleId = dependency.Id
        });
        await dbContext.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = new JobRoleService(dbContext, mapperMock.Object);

        await Assert.ThrowsAsync<AlreadyExistsException>(() => service.AddDependenciesToJobRole(jobRole.Id, dependency.Id));
    }

    [Fact]
    public async Task AddDependenciesToJobRole_SelfDependency_ThrowsInvalidOperationException()
    {
        await using var dbContext = CreateDbContext();
        var role = CreateJobRole(1, "Nurse");
        dbContext.JobRoles.Add(role);
        await dbContext.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = new JobRoleService(dbContext, mapperMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddDependenciesToJobRole(role.Id, role.Id));
    }

    [Fact]
    public async Task AddDependenciesToJobRole_Success_AddsDependency()
    {
        await using var dbContext = CreateDbContext();
        var jobRole = CreateJobRole(1, "Nurse");
        var dependency = CreateJobRole(2, "Doctor");
        dbContext.JobRoles.AddRange(jobRole, dependency);
        await dbContext.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = new JobRoleService(dbContext, mapperMock.Object);

        await service.AddDependenciesToJobRole(jobRole.Id, dependency.Id);

        var saved = await dbContext.JobRoleDependencies
            .Include(x => x.JobRole)
            .Include(x => x.Dependency)
            .SingleAsync();

        Assert.Equal(jobRole.Id, saved.JobRole.Id);
        Assert.Equal(dependency.Id, saved.Dependency.Id);
    }

    [Fact]
    public async Task RemoveDependenciesToJobRole_RemovesWhenExists()
    {
        await using var dbContext = CreateDbContext();
        var jobRole = CreateJobRole(1, "Nurse");
        var dependency = CreateJobRole(2, "Doctor");
        dbContext.JobRoles.AddRange(jobRole, dependency);
        dbContext.JobRoleDependencies.Add(new JobRoleDependency
        {
            JobRoleId = jobRole.Id,
            DependencyJobRoleId = dependency.Id
        });
        await dbContext.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = new JobRoleService(dbContext, mapperMock.Object);

        await service.RemoveDependenciesToJobRole(jobRole.Id, dependency.Id);

        var remaining = await dbContext.JobRoleDependencies.ToListAsync();
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task RemoveDependenciesToJobRole_JobRoleNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = new Mock<IMapper>();
        var service = new JobRoleService(dbContext, mapperMock.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => service.RemoveDependenciesToJobRole(999, 1));
    }

    [Fact]
    public async Task AddUsersToJobRoleAsync_JobRoleNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = new Mock<IMapper>();
        var service = new JobRoleService(dbContext, mapperMock.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => service.AddUsersToJobRoleAsync(999, new List<int> { 1 }));
    }

    [Fact]
    public async Task AddUsersToJobRoleAsync_UserNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        dbContext.JobRoles.Add(CreateJobRole(1, "Nurse"));
        await dbContext.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = new JobRoleService(dbContext, mapperMock.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => service.AddUsersToJobRoleAsync(1, new List<int> { 999 }));
    }

    [Fact]
    public async Task AddUsersToJobRoleAsync_AddsMissingRoleToUsers()
    {
        await using var dbContext = CreateDbContext();
        var role = CreateJobRole(1, "Nurse");
        var user1 = CreateUserWithId(1);
        var user2 = CreateUserWithId(2);

        dbContext.JobRoles.Add(role);
        dbContext.Users.AddRange(user1, user2);
        await dbContext.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = new JobRoleService(dbContext, mapperMock.Object);

        await service.AddUsersToJobRoleAsync(role.Id, new List<int> { (int)user1.Id, (int)user2.Id });

        var updatedUser1 = await dbContext.Users.Include(x => x.JobRoles).FirstAsync(x => x.Id == user1.Id);
        var updatedUser2 = await dbContext.Users.Include(x => x.JobRoles).FirstAsync(x => x.Id == user2.Id);

        Assert.Contains(updatedUser1.JobRoles, x => x.JobRoleId == role.Id);
        Assert.Contains(updatedUser2.JobRoles, x => x.JobRoleId == role.Id);
    }

    [Fact]
    public async Task RemoveUsersFromJobRoleAsync_RemovesRoleWhenPresent()
    {
        await using var dbContext = CreateDbContext();
        var role = CreateJobRole(1, "Nurse");
        var user = CreateUserWithId(1);
        user.JobRoles.Add(new UserJobRoles
        {
            User = user,
            UserId = (int)user.Id,
            JobRole = role,
            JobRoleId = role.Id
        });

        dbContext.JobRoles.Add(role);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = new JobRoleService(dbContext, mapperMock.Object);

        await service.RemoveUsersFromJobRoleAsync(role.Id, new List<int> { (int)user.Id });

        var updated = await dbContext.Users.Include(x => x.JobRoles).FirstAsync(x => x.Id == user.Id);
        Assert.Empty(updated.JobRoles);
    }

    [Fact]
    public async Task GetJobRoleAsync_NotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = new Mock<IMapper>();
        var service = new JobRoleService(dbContext, mapperMock.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetJobRoleAsync(999));
    }

    [Fact]
    public async Task GetJobRoleAsync_ReturnsMappedDto()
    {
        await using var dbContext = CreateDbContext();
        var role = CreateJobRole(1, "Nurse");
        dbContext.JobRoles.Add(role);
        await dbContext.SaveChangesAsync();

        var expectedDto = new JobRoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            DependentOn = new List<JobRoleDto>(),
            Prerequisites = new List<JobRoleDto>(),
            Users = new List<UserDto>()
        };

        var mapperMock = new Mock<IMapper>();
        mapperMock
            .Setup(m => m.Map<JobRole, JobRoleDto>(It.IsAny<JobRole>()))
            .Returns(expectedDto);

        var service = new JobRoleService(dbContext, mapperMock.Object);

        var result = await service.GetJobRoleAsync(role.Id);

        Assert.Equal(expectedDto.Id, result.Id);
        Assert.Equal(expectedDto.Name, result.Name);
        Assert.Equal(expectedDto.Description, result.Description);
    }

    [Fact]
    public async Task GetJobRolesAsync_ReturnsPagedAndFilteredResults()
    {
        await using var dbContext = CreateDbContext();
        dbContext.JobRoles.AddRange(
            CreateJobRole(1, "Nurse"),
            CreateJobRole(2, "Doctor"),
            CreateJobRole(3, "Assistant"));
        await dbContext.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        mapperMock
            .Setup(m => m.Map<JobRole, JobRoleShortDto>(It.IsAny<JobRole>()))
            .Returns((JobRole role) => new JobRoleShortDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                UsersWithRoleCount = 0
            });

        var service = new JobRoleService(dbContext, mapperMock.Object);

        var result = await service.GetJobRolesAsync(
            new PaginationDto { Page = 1, PageSize = 2 },
            "n");

        var roles = result.JobRoles.ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(2, roles.Count);
        Assert.All(roles, r => Assert.Contains("n", r.Name, StringComparison.OrdinalIgnoreCase));
    }

    private static SchichtpilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SchichtpilotDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SchichtpilotDbContext(options);
    }

    private static JobRole CreateJobRole(int id, string name)
    {
        return new JobRole
        {
            Id = id,
            Name = name,
            Description = $"{name} description",
            CreatedOn = DateTime.UtcNow,
            UsersWithRole = new HashSet<UserJobRoles>(),
            Dependencies = new HashSet<JobRoleDependency>(),
            Prerequisites = new HashSet<JobRoleDependency>()
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
            BirthDate = new DateTime(1990, 1, 1),
            JobRoles = new HashSet<UserJobRoles>()
        };
    }
}
