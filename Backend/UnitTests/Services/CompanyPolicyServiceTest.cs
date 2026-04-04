using AutoMapper;
using Data;
using Data.Entities;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Moq;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Services;
using Schichtpilot.Exceptions;
using Schichtpilot.Interfaces;

namespace UnitTests.Services;

[TestSubject(typeof(CompanyPolicyService))]
public class CompanyPolicyServiceTest
{
    [Fact]
    public async Task AddHolidaysAsync_NewHoliday_AddsHoliday()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = new Mock<IMapper>();
        var service = CreateService(dbContext, mapperMock);

        var dto = new HolidaysDto
        {
            Holidays = new List<DateTime> { new DateTime(2025, 1, 1) }
        };

        await service.AddHolidaysAsync(dto);

        var holidays = await dbContext.Holidays.ToListAsync();
        Assert.Single(holidays);
        Assert.Equal(new DateTime(2025, 1, 1), holidays[0].Date.Date);
    }

    [Fact]
    public async Task AddHolidaysAsync_ExistingHoliday_DoesNotDuplicateHoliday()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Holidays.Add(new Holiday { Date = new DateTime(2025, 1, 1) });
        await dbContext.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = CreateService(dbContext, mapperMock);

        var dto = new HolidaysDto
        {
            Holidays = new List<DateTime> { new DateTime(2025, 1, 1) }
        };

        await service.AddHolidaysAsync(dto);

        var holidays = await dbContext.Holidays.ToListAsync();
        Assert.Single(holidays);
        Assert.Equal(new DateTime(2025, 1, 1), holidays[0].Date.Date);
    }

    [Fact]
    public async Task RemoveHolidaysAsync_ExistingHoliday_RemovesHoliday()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Holidays.Add(new Holiday { Date = new DateTime(2025, 12, 24) });
        await dbContext.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = CreateService(dbContext, mapperMock);

        var dto = new HolidaysDto
        {
            Holidays = new List<DateTime> { new DateTime(2025, 12, 24) }
        };

        await service.RemoveHolidaysAsync(dto);

        var holidays = await dbContext.Holidays.ToListAsync();
        Assert.Empty(holidays);
    }

    [Fact]
    public async Task RemoveHolidaysAsync_NonExistingHoliday_DoesNothing()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Holidays.Add(new Holiday { Date = new DateTime(2025, 12, 25) });
        await dbContext.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = CreateService(dbContext, mapperMock);

        var dto = new HolidaysDto
        {
            Holidays = new List<DateTime> { new DateTime(2025, 1, 1) }
        };

        await service.RemoveHolidaysAsync(dto);

        var holidays = await dbContext.Holidays.ToListAsync();
        Assert.Single(holidays);
        Assert.Equal(new DateTime(2025, 12, 25), holidays[0].Date.Date);
    }

    [Fact]
    public async Task GetHolidaysAsync_ReturnsAllHolidayDates()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Holidays.AddRange(
            new Holiday { Date = new DateTime(2025, 1, 1) },
            new Holiday { Date = new DateTime(2025, 12, 25) });
        await dbContext.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = CreateService(dbContext, mapperMock);

        var result = await service.GetHolidaysAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Holidays.Count);
        Assert.Contains(new DateTime(2025, 1, 1), result.Holidays);
        Assert.Contains(new DateTime(2025, 12, 25), result.Holidays);
    }

    [Fact]
    public async Task SetPolicyAsync_WhenNoPolicyExists_CreatesPolicy()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = new Mock<IMapper>();
        var service = CreateService(dbContext, mapperMock);

        var dto = new CompanyPolicyDto
        {
            MaximumConsecutiveWorkHoursPerDay = 10,
            MinimumRestPeriodInMinutes = 30,
            RestPeriodThresholdInMinutes = 360,
            MaximumConsecutiveWorkHoursPerWeek = 48
        };

        await service.SetPolicyAsync(dto);

        var saved = await dbContext.WorkPolicies.SingleOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal(dto.MaximumConsecutiveWorkHoursPerDay, saved.MaximumConsecutiveWorkHoursPerDay);
        Assert.Equal(dto.MinimumRestPeriodInMinutes, saved.MinimumRestPeriodInMinutes);
        Assert.Equal(dto.RestPeriodThresholdInMinutes, saved.RestPeriodThresholdInMinutes);
        Assert.Equal(dto.MaximumConsecutiveWorkHoursPerWeek, saved.MaximumConsecutiveWorkHoursPerWeek);
    }

    [Fact]
    public async Task SetPolicyAsync_WhenPolicyExists_UpdatesPolicy()
    {
        await using var dbContext = CreateDbContext();
        dbContext.WorkPolicies.Add(new WorkPolicy
        {
            MaximumConsecutiveWorkHoursPerDay = 8,
            MinimumRestPeriodInMinutes = 15,
            RestPeriodThresholdInMinutes = 300,
            MaximumConsecutiveWorkHoursPerWeek = 40
        });
        await dbContext.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var scheduleServiceMock = new Mock<IWorkScheduleService>();

        var scheduleA = new WorkSchedule
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
        var scheduleB = new WorkSchedule
        {
            Id = 11,
            Name = "Schedule B",
            StartDate = new DateTime(2026, 1, 8),
            EndDate = new DateTime(2026, 1, 14),
            IsActive = true,
            IsValid = true,
            Shifts = new HashSet<WorkScheduleShifts>(),
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };

        dbContext.WorkSchedules.AddRange(scheduleA, scheduleB);
        await dbContext.SaveChangesAsync();

        scheduleServiceMock
            .Setup(x => x.SetScheduleOfflineAsync(scheduleA.Id))
            .Returns(Task.CompletedTask);
        scheduleServiceMock
            .Setup(x => x.SetScheduleAsInvalidAsync(scheduleA.Id))
            .Returns(Task.CompletedTask);
        scheduleServiceMock
            .Setup(x => x.SetScheduleOfflineAsync(scheduleB.Id))
            .Returns(Task.CompletedTask);
        scheduleServiceMock
            .Setup(x => x.SetScheduleAsInvalidAsync(scheduleB.Id))
            .Returns(Task.CompletedTask);

        var service = CreateService(dbContext, mapperMock, scheduleServiceMock);

        var dto = new CompanyPolicyDto
        {
            MaximumConsecutiveWorkHoursPerDay = 12,
            MinimumRestPeriodInMinutes = 45,
            RestPeriodThresholdInMinutes = 420,
            MaximumConsecutiveWorkHoursPerWeek = 50
        };

        await service.SetPolicyAsync(dto);

        var policies = await dbContext.WorkPolicies.ToListAsync();
        Assert.Single(policies);

        var updated = policies[0];
        Assert.Equal(dto.MaximumConsecutiveWorkHoursPerDay, updated.MaximumConsecutiveWorkHoursPerDay);
        Assert.Equal(dto.MinimumRestPeriodInMinutes, updated.MinimumRestPeriodInMinutes);
        Assert.Equal(dto.RestPeriodThresholdInMinutes, updated.RestPeriodThresholdInMinutes);
        Assert.Equal(dto.MaximumConsecutiveWorkHoursPerWeek, updated.MaximumConsecutiveWorkHoursPerWeek);

        scheduleServiceMock.Verify(x => x.SetScheduleOfflineAsync(scheduleA.Id), Times.Once);
        scheduleServiceMock.Verify(x => x.SetScheduleAsInvalidAsync(scheduleA.Id), Times.Once);
        scheduleServiceMock.Verify(x => x.SetScheduleOfflineAsync(scheduleB.Id), Times.Once);
        scheduleServiceMock.Verify(x => x.SetScheduleAsInvalidAsync(scheduleB.Id), Times.Once);
    }

    [Fact]
    public async Task GetPolicyAsync_ReturnsMappedPolicyDto()
    {
        await using var dbContext = CreateDbContext();
        var entity = new WorkPolicy
        {
            MaximumConsecutiveWorkHoursPerDay = 9,
            MinimumRestPeriodInMinutes = 20,
            RestPeriodThresholdInMinutes = 330,
            MaximumConsecutiveWorkHoursPerWeek = 45
        };
        dbContext.WorkPolicies.Add(entity);
        await dbContext.SaveChangesAsync();

        var expected = new CompanyPolicyDto
        {
            MaximumConsecutiveWorkHoursPerDay = 9,
            MinimumRestPeriodInMinutes = 20,
            RestPeriodThresholdInMinutes = 330,
            MaximumConsecutiveWorkHoursPerWeek = 45
        };

        var mapperMock = new Mock<IMapper>();
        mapperMock
            .Setup(m => m.Map<CompanyPolicyDto>(It.IsAny<WorkPolicy>()))
            .Returns(expected);

        var service = CreateService(dbContext, mapperMock);

        var result = await service.GetPolicyAsync();

        Assert.NotNull(result);
        Assert.Equal(expected.MaximumConsecutiveWorkHoursPerDay, result.MaximumConsecutiveWorkHoursPerDay);
        Assert.Equal(expected.MinimumRestPeriodInMinutes, result.MinimumRestPeriodInMinutes);
        Assert.Equal(expected.RestPeriodThresholdInMinutes, result.RestPeriodThresholdInMinutes);
        Assert.Equal(expected.MaximumConsecutiveWorkHoursPerWeek, result.MaximumConsecutiveWorkHoursPerWeek);
    }

    [Fact]
    public async Task GetPolicyAsync_WhenNoPolicyExists_ThrowsNotSetException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = new Mock<IMapper>();
        var service = CreateService(dbContext, mapperMock);

        await Assert.ThrowsAsync<NotSetException>(() => service.GetPolicyAsync());
    }

    private static CompanyPolicyService CreateService(
        SchichtpilotDbContext dbContext,
        Mock<IMapper> mapperMock,
        Mock<IWorkScheduleService>? workScheduleServiceMock = null)
    {
        var workScheduleService = workScheduleServiceMock ?? new Mock<IWorkScheduleService>();
        return new CompanyPolicyService(dbContext, mapperMock.Object, workScheduleService.Object);
    }

    private static SchichtpilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SchichtpilotDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SchichtpilotDbContext(options);
    }
}
