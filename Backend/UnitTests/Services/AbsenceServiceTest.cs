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
            () => service.CreateAbsenceRequestAsync(999, dto));
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
            Message = "Past vacation",
            AbsenceType = nameof(AbsenceTypeEnum.EducationalLeave)
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
            () => service.CreateAbsenceRequestAsync(1, dto));
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
            Status = nameof(AbsenceStatusEnum.Approved),
            AbsenceType = nameof(AbsenceTypeEnum.EducationalLeave)
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
            () => service.CreateAbsenceRequestAsync(user.Id, dto));
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
            ManagerMessage = "Test",
            AbsenceType = nameof(AbsenceTypeEnum.EducationalLeave)
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

        await service.CreateAbsenceRequestAsync(user.Id, dto);

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

        await service.CreateAbsenceRequestAsync(user.Id, dto);

        var absence = await dbContext.Absences.FirstOrDefaultAsync();
        Assert.NotNull(absence);
        Assert.Equal(nameof(AbsenceStatusEnum.Pending), absence.Status);
        Assert.Equal(dto.StartDate, absence.StartDate);

        _workScheduleServiceMock.Verify(x => x.SetScheduleOfflineAsync(It.IsAny<int>()), Times.Never);
        _workScheduleServiceMock.Verify(x => x.SetScheduleAsInvalidAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CreateAbsenceRequestAsync_SickLeave_CreatesApprovedAbsenceAndUpdatesSchedules()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateCompleteUser(1);
        user.JobRoles = new HashSet<UserJobRoles>();

        var role = new JobRole
        {
            Id = 1,
            Name = "Nurse",
            Description = "Nurse role",
            CreatedOn = DateTime.UtcNow,
            UsersWithRole = new HashSet<UserJobRoles>(),
            Dependencies = new HashSet<JobRoleDependency>(),
            Prerequisites = new HashSet<JobRoleDependency>()
        };

        var userJobRole = new UserJobRoles
        {
            User = user,
            UserId = user.Id,
            JobRole = role,
            JobRoleId = role.Id
        };
        user.JobRoles.Add(userJobRole);

        var schedule = new WorkSchedule
        {
            Id = 10,
            Name = "Schedule A",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 7),
            IsActive = true,
            IsValid = true,
            Shifts = new HashSet<WorkScheduleShifts>(),
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };

        var assignment = new ShiftAssignment
        {
            WorkSchedule = schedule,
            WorkScheduleId = schedule.Id,
            UserJobRole = userJobRole,
            UserId = user.Id,
            JobRoleId = role.Id,
            TimeslotId = 1,
            StartTime = new DateTime(2026, 1, 2, 8, 0, 0),
            EndTime = new DateTime(2026, 1, 3, 12, 0, 0)
        };
        schedule.ShiftAssignments.Add(assignment);

        dbContext.JobRoles.Add(role);
        dbContext.Users.Add(user);
        dbContext.UserJobRoles.Add(userJobRole);
        dbContext.WorkSchedules.Add(schedule);
        dbContext.ShiftAssignments.Add(assignment);
        await dbContext.SaveChangesAsync();

        _workScheduleServiceMock
            .Setup(x => x.SetScheduleOfflineAsync(schedule.Id))
            .Returns(Task.CompletedTask);
        _workScheduleServiceMock
            .Setup(x => x.SetScheduleAsInvalidAsync(schedule.Id))
            .Returns(Task.CompletedTask);

        var service = CreateService(dbContext);
        var dto = new CreateAbsenceDto
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 4),
            AbsenceType = nameof(AbsenceTypeEnum.SickLeave),
            Message = "Sick leave"
        };

        await service.CreateAbsenceRequestAsync(user.Id, dto);

        var absence = await dbContext.Absences.FirstOrDefaultAsync();
        Assert.NotNull(absence);
        Assert.Equal(nameof(AbsenceStatusEnum.Approved), absence.Status);
        Assert.Equal(dto.StartDate.Date, absence.StartDate);

        _workScheduleServiceMock.Verify(x => x.SetScheduleOfflineAsync(schedule.Id), Times.Once);
        _workScheduleServiceMock.Verify(x => x.SetScheduleAsInvalidAsync(schedule.Id), Times.Once);
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
        dbContext.Absences.Add(new Absence { Id = 1, UserId = 1, Status = nameof(AbsenceStatusEnum.Approved), AbsenceType = nameof(AbsenceTypeEnum.EducationalLeave) });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.DeleteOwnAbsenceAsync(1, 1));
    }

    [Fact]
    public async Task DeleteOwnAbsenceAsync_OwnPendingAbsence_DeletesSuccessfully()
    {
        await using var dbContext = CreateDbContext();
        var absence = new Absence { Id = 1, UserId = 1, Status = nameof(AbsenceStatusEnum.Pending), AbsenceType = nameof(AbsenceTypeEnum.EducationalLeave) };
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
        dbContext.Absences.Add(new Absence { Id = 1, Status = nameof(AbsenceStatusEnum.Approved), AbsenceType = nameof(AbsenceTypeEnum.EducationalLeave) });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var dto = new StatusUpdateDto { Status = nameof(AbsenceStatusEnum.Approved) };

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateAbsenceStatusAsync(1, dto));
    }

    [Fact]
    public async Task UpdateAbsenceStatusAsync_Approved_UpdatesSchedulesAndSavesChanges()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateCompleteUser(1);
        user.JobRoles = new HashSet<UserJobRoles>();

        var role = new JobRole
        {
            Id = 1,
            Name = "Nurse",
            Description = "Nurse role",
            CreatedOn = DateTime.UtcNow,
            UsersWithRole = new HashSet<UserJobRoles>(),
            Dependencies = new HashSet<JobRoleDependency>(),
            Prerequisites = new HashSet<JobRoleDependency>()
        };

        var userJobRole = new UserJobRoles
        {
            User = user,
            UserId = user.Id,
            JobRole = role,
            JobRoleId = role.Id
        };
        user.JobRoles.Add(userJobRole);

        var schedule = new WorkSchedule
        {
            Id = 10,
            Name = "Schedule A",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 7),
            IsActive = true,
            IsValid = true,
            Shifts = new HashSet<WorkScheduleShifts>(),
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };

        var assignment = new ShiftAssignment
        {
            WorkSchedule = schedule,
            WorkScheduleId = schedule.Id,
            UserJobRole = userJobRole,
            UserId = user.Id,
            JobRoleId = role.Id,
            TimeslotId = 1,
            StartTime = new DateTime(2026, 1, 2, 8, 0, 0),
            EndTime = new DateTime(2026, 1, 3, 12, 0, 0)
        };
        schedule.ShiftAssignments.Add(assignment);

        var absence = new Absence
        {
            Id = 1,
            UserId = user.Id,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 4),
            Status = nameof(AbsenceStatusEnum.Pending),
            AbsenceType = nameof(AbsenceTypeEnum.EducationalLeave)
        };

        dbContext.JobRoles.Add(role);
        dbContext.Users.Add(user);
        dbContext.UserJobRoles.Add(userJobRole);
        dbContext.WorkSchedules.Add(schedule);
        dbContext.ShiftAssignments.Add(assignment);
        dbContext.Absences.Add(absence);
        await dbContext.SaveChangesAsync();

        _workScheduleServiceMock
            .Setup(x => x.SetScheduleOfflineAsync(schedule.Id))
            .Returns(Task.CompletedTask);
        _workScheduleServiceMock
            .Setup(x => x.SetScheduleAsInvalidAsync(schedule.Id))
            .Returns(Task.CompletedTask);

        var service = CreateService(dbContext);
        var dto = new StatusUpdateDto
        {
            Status = nameof(AbsenceStatusEnum.Approved),
            ManagerMessage = "Approved by manager"
        };

        await service.UpdateAbsenceStatusAsync(absence.Id, dto);

        var updated = await dbContext.Absences.FindAsync(absence.Id);
        Assert.Equal(nameof(AbsenceStatusEnum.Approved), updated.Status);
        Assert.Equal("Approved by manager", updated.ManagerMessage);

        _workScheduleServiceMock.Verify(x => x.SetScheduleOfflineAsync(schedule.Id), Times.Once);
        _workScheduleServiceMock.Verify(x => x.SetScheduleAsInvalidAsync(schedule.Id), Times.Once);
    }

    [Fact]
    public async Task UpdateAbsenceStatusAsync_Denied_DoesNotUpdateSchedules()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateCompleteUser(1);
        user.JobRoles = new HashSet<UserJobRoles>();

        var role = new JobRole
        {
            Id = 1,
            Name = "Nurse",
            Description = "Nurse role",
            CreatedOn = DateTime.UtcNow,
            UsersWithRole = new HashSet<UserJobRoles>(),
            Dependencies = new HashSet<JobRoleDependency>(),
            Prerequisites = new HashSet<JobRoleDependency>()
        };

        var userJobRole = new UserJobRoles
        {
            User = user,
            UserId = user.Id,
            JobRole = role,
            JobRoleId = role.Id
        };
        user.JobRoles.Add(userJobRole);

        var schedule = new WorkSchedule
        {
            Id = 10,
            Name = "Schedule A",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 7),
            IsActive = true,
            IsValid = true,
            Shifts = new HashSet<WorkScheduleShifts>(),
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };

        var assignment = new ShiftAssignment
        {
            WorkSchedule = schedule,
            WorkScheduleId = schedule.Id,
            UserJobRole = userJobRole,
            UserId = user.Id,
            JobRoleId = role.Id,
            TimeslotId = 1,
            StartTime = new DateTime(2026, 1, 2, 8, 0, 0),
            EndTime = new DateTime(2026, 1, 3, 12, 0, 0)
        };
        schedule.ShiftAssignments.Add(assignment);

        var absence = new Absence
        {
            Id = 1,
            UserId = user.Id,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 4),
            Status = nameof(AbsenceStatusEnum.Pending),
            AbsenceType = nameof(AbsenceTypeEnum.EducationalLeave)
        };

        dbContext.JobRoles.Add(role);
        dbContext.Users.Add(user);
        dbContext.UserJobRoles.Add(userJobRole);
        dbContext.WorkSchedules.Add(schedule);
        dbContext.ShiftAssignments.Add(assignment);
        dbContext.Absences.Add(absence);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var dto = new StatusUpdateDto
        {
            Status = nameof(AbsenceStatusEnum.Denied),
            ManagerMessage = "Not approved"
        };

        await service.UpdateAbsenceStatusAsync(absence.Id, dto);

        var updated = await dbContext.Absences.FindAsync(absence.Id);
        Assert.Equal(nameof(AbsenceStatusEnum.Denied), updated.Status);
        Assert.Equal("Not approved", updated.ManagerMessage);

        _workScheduleServiceMock.Verify(x => x.SetScheduleOfflineAsync(It.IsAny<int>()), Times.Never);
        _workScheduleServiceMock.Verify(x => x.SetScheduleAsInvalidAsync(It.IsAny<int>()), Times.Never);
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
