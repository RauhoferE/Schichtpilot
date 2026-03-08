using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using AutoMapper;
using Data;
using Data.Entities;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Schichtpilot.Exceptions;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Enums;
using Schichtpilot.Services;

namespace UnitTests.Services;

[TestSubject(typeof(AbsenceService))]
public class AbsenceServiceTest
{
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IWorkScheduleService> _workScheduleServiceMock;

    public AbsenceServiceTest()
    {
        _mapperMock = new Mock<IMapper>();

        _mapperMock
            .Setup(m => m.ProjectTo<AbsenceDto>(
                It.IsAny<IQueryable<Absence>>(),
                It.IsAny<object>(),
                It.IsAny<Expression<Func<AbsenceDto, object>>[]>()))
            .Returns((IQueryable<Absence> src, object parameters, Expression<Func<AbsenceDto, object>>[] members) =>
                src.Select(e => new AbsenceDto
                {
                    Id = e.Id,
                    UserId = e.UserId,
                    UserName = e.User.UserName ?? "Unknown",
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    Message = e.Message,
                    // CreatedAt = e.CreatedAt,
                    ManagerMessage = e.ManagerMessage ?? "",
                }).AsQueryable()
            );

        _emailServiceMock = new Mock<IEmailService>();
        _workScheduleServiceMock = new Mock<IWorkScheduleService>();
    }
    private static SchichtpilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SchichtpilotDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new SchichtpilotDbContext(options);
    }

    private AbsenceService CreateService(SchichtpilotDbContext dbContext)
    {
        return new AbsenceService(dbContext, _mapperMock.Object, _emailServiceMock.Object, _workScheduleServiceMock.Object);
    }

    [Fact]
    public async Task CreateAbsenceRequestAsync_UserNotFound_ThrowsUserNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var dto = new CreateAbsenceDto
        {
            StartDate = DateTime.UtcNow.AddDays(1).Date,
            EndDate = DateTime.UtcNow.AddDays(2).Date,
            AbsenceType = nameof(AbsenceTypeEnum.Vacation),
        };

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => service.CreateAbsenceRequestAsync(dto, 999));
    }

    [Fact]
    public async Task CreateAbsenceRequestAsync_OverlappingPastAbsence_ThrowsAlreadyExistsException()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateCompleteUser(1);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // PAST overlapping absence (Approved)
        var pastOverlap = new Absence
        {
            UserId = 1,
            StartDate = DateTime.UtcNow.AddDays(-5).Date,
            EndDate = DateTime.UtcNow.AddDays(-3).Date,
            Status = nameof(AbsenceStatusEnum.Approved),
            Message = "Past vacation"
        };
        dbContext.Absences.Add(pastOverlap);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var dto = new CreateAbsenceDto
        {
            StartDate = DateTime.UtcNow.AddDays(-4).Date,  // Overlaps past
            EndDate = DateTime.UtcNow.AddDays(-2).Date,
            AbsenceType = nameof(AbsenceTypeEnum.Vacation),
            Message = "New request"  // Required
        };

        await Assert.ThrowsAsync<AlreadyExistsException>(
            () => service.CreateAbsenceRequestAsync(dto, 1));
    }


    [Fact]
    public async Task CreateAbsenceRequestAsync_OverlappingApprovedAbsence_ThrowsAlreadyExistsException()
    {
        await using var dbContext = CreateDbContext();

        var user = CreateCompleteUser(1);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var overlapping = new Absence
        {
            UserId = user.Id,
            StartDate = DateTime.UtcNow.AddDays(2).Date,
            EndDate = DateTime.UtcNow.AddDays(4).Date,
            Status = nameof(AbsenceStatusEnum.Approved)
        };
        dbContext.Absences.Add(overlapping);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var dto = new CreateAbsenceDto
        {
            StartDate = DateTime.UtcNow.AddDays(3).Date,
            EndDate = DateTime.UtcNow.AddDays(5).Date,
            AbsenceType = nameof(AbsenceTypeEnum.Vacation)
        };

        await Assert.ThrowsAsync<AlreadyExistsException>(
            () => service.CreateAbsenceRequestAsync(dto, user.Id));
    }

    [Fact]
    public async Task CreateAbsenceRequestAsync_OverlappingDeniedAbsence_AllowsCreation()
    {
        await using var dbContext = CreateDbContext();

        var user = CreateCompleteUser(1);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var overlapping = new Absence
        {
            UserId = user.Id,
            StartDate = DateTime.UtcNow.AddDays(2).Date,
            EndDate = DateTime.UtcNow.AddDays(4).Date,
            Status = nameof(AbsenceStatusEnum.Denied),
            ManagerMessage = "Test"
        };
        dbContext.Absences.Add(overlapping);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var dto = new CreateAbsenceDto
        {
            StartDate = DateTime.UtcNow.AddDays(3).Date,
            EndDate = DateTime.UtcNow.AddDays(5).Date,
            AbsenceType = nameof(AbsenceTypeEnum.Vacation),
            Message = "Test"
        };

        await service.CreateAbsenceRequestAsync(dto, user.Id);

        var absences = await dbContext.Absences.Where(x => x.UserId == user.Id).ToListAsync();
        Assert.Equal(2, absences.Count);
    }


    [Fact]
    public async Task CreateAbsenceRequestAsync_CreatesPendingAbsence()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateCompleteUser(1);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var dto = new CreateAbsenceDto
        {
            StartDate = DateTime.UtcNow.AddDays(1).Date,
            EndDate = DateTime.UtcNow.AddDays(2).Date,
            AbsenceType = nameof(AbsenceTypeEnum.Vacation),
            Message = "Vacation request"
        };

        await service.CreateAbsenceRequestAsync(dto, user.Id);

        var absence = await dbContext.Absences.FirstOrDefaultAsync();
        Assert.NotNull(absence);
        Assert.Equal(nameof(AbsenceStatusEnum.Pending), absence.Status);
        Assert.Equal(dto.StartDate, absence.StartDate);
    }

    [Fact]
    public async Task CreateAbsenceRequestAsync_SickLeave_CreatesApprovedAbsence()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateCompleteUser(1);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var dto = new CreateAbsenceDto
        {
            StartDate = DateTime.UtcNow.AddDays(1).Date,
            EndDate = DateTime.UtcNow.AddDays(2).Date,
            AbsenceType = nameof(AbsenceTypeEnum.SickLeave),
            Message = "Sick leave"
        };

        await service.CreateAbsenceRequestAsync(dto, user.Id);

        var absence = await dbContext.Absences.FirstOrDefaultAsync();
        Assert.NotNull(absence);
        Assert.Equal(nameof(AbsenceStatusEnum.Approved), absence.Status);
        Assert.Equal(dto.StartDate, absence.StartDate);
    }

    [Fact]
    public async Task DeleteOwnAbsenceAsync_NotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.DeleteOwnAbsenceAsync(999, 1));
    }

    [Fact]
    public async Task DeleteOwnAbsenceAsync_ApprovedAbsence_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Absences.Add(new Absence { Id = 1, UserId = 1, Status = nameof(AbsenceStatusEnum.Approved) });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.DeleteOwnAbsenceAsync(1, 1));
    }

    [Fact]
    public async Task DeleteOwnAbsenceAsync_OwnPendingAbsence_DeletesSuccessfully()
    {
        await using var dbContext = CreateDbContext();
        var absence = new Absence { Id = 1, UserId = 1, Status = nameof(AbsenceStatusEnum.Pending) };
        dbContext.Absences.Add(absence);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await service.DeleteOwnAbsenceAsync(1, 1);

        var remaining = await dbContext.Absences.FindAsync(1);
        Assert.Null(remaining);
    }

    [Fact]
    public async Task UpdateAbsenceStatusAsync_NotPending_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Absences.Add(new Absence { Id = 1, Status = nameof(AbsenceStatusEnum.Approved) });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var dto = new StatusUpdateDto { Status = nameof(AbsenceStatusEnum.Approved) };

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateAbsenceStatusAsync(1, dto));
    }

    [Fact]
    public async Task UpdateAbsenceStatusAsync_DeniedWithoutMessage_ThrowsValidationException()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Absences.Add(new Absence { Id = 1, Status = nameof(AbsenceStatusEnum.Pending) });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var dto = new StatusUpdateDto { Status = nameof(AbsenceStatusEnum.Denied) };

        await Assert.ThrowsAsync<ValidationException>(
            () => service.UpdateAbsenceStatusAsync(1, dto));
    }

    [Fact]
    public async Task UpdateAbsenceStatusAsync_ValidUpdate_SavesChanges()
    {
        await using var dbContext = CreateDbContext();
        var absence = new Absence { Id = 1, Status = nameof(AbsenceStatusEnum.Pending) };
        dbContext.Absences.Add(absence);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var dto = new StatusUpdateDto { Status = nameof(AbsenceStatusEnum.Approved), ManagerMessage = "Approved by manager" };

        await service.UpdateAbsenceStatusAsync(1, dto);

        var updated = await dbContext.Absences.FindAsync(1);
        Assert.Equal(nameof(AbsenceStatusEnum.Approved), updated.Status);
        Assert.Equal("Approved by manager", updated.ManagerMessage);
    }

    [Fact]
    public async Task GetAbsenceDetailAsync_NotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetAbsenceDetailAsync(999));
    }

    private User CreateCompleteUser(int id)
    {
        return new User
        {
            Id = id,
            FirstName = "John",
            LastName = "Doe",
            Email = $"john.doe{id}@test.com",
            UserName = $"john.doe{id}@test.com",
            City = "Testville",
            StreetAddress = "Main St 1",
            PostalCode = 12345,
            EmailConfirmed = true
        };
    }
}
