using AutoMapper;
using Data;
using Data.Entities;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Moq;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Services;
using Schichtpilot.Exceptions;

namespace UnitTests.Services;

[TestSubject(typeof(CompanyPolicyService))]
public class CompanyPolicyServiceTest
{
    [Fact]
    public async Task AddHolidaysAsync_NewHoliday_AddsHoliday()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = new Mock<IMapper>();
        var service = new CompanyPolicyService(dbContext, mapperMock.Object);

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
        var service = new CompanyPolicyService(dbContext, mapperMock.Object);

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
        var service = new CompanyPolicyService(dbContext, mapperMock.Object);

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
        var service = new CompanyPolicyService(dbContext, mapperMock.Object);

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
        var service = new CompanyPolicyService(dbContext, mapperMock.Object);

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
        var service = new CompanyPolicyService(dbContext, mapperMock.Object);

        var dto = new CompanyPolicyDto
        {
            MaximumConsecutiveWorkHoursPerDay = 10,
            RestPeriodInMinutes = 30,
            RestPeriodThresholdInMinutes = 360,
            MaximumConsecutiveWorkHoursPerWeek = 48
        };

        await service.SetPolicyAsync(dto);

        var saved = await dbContext.WorkPolicies.SingleOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal(dto.MaximumConsecutiveWorkHoursPerDay, saved.MaximumConsecutiveWorkHoursPerDay);
        Assert.Equal(dto.RestPeriodInMinutes, saved.RestPeriodInMinutes);
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
            RestPeriodInMinutes = 15,
            RestPeriodThresholdInMinutes = 300,
            MaximumConsecutiveWorkHoursPerWeek = 40
        });
        await dbContext.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = new CompanyPolicyService(dbContext, mapperMock.Object);

        var dto = new CompanyPolicyDto
        {
            MaximumConsecutiveWorkHoursPerDay = 12,
            RestPeriodInMinutes = 45,
            RestPeriodThresholdInMinutes = 420,
            MaximumConsecutiveWorkHoursPerWeek = 50
        };

        await service.SetPolicyAsync(dto);

        var policies = await dbContext.WorkPolicies.ToListAsync();
        Assert.Single(policies);

        var updated = policies[0];
        Assert.Equal(dto.MaximumConsecutiveWorkHoursPerDay, updated.MaximumConsecutiveWorkHoursPerDay);
        Assert.Equal(dto.RestPeriodInMinutes, updated.RestPeriodInMinutes);
        Assert.Equal(dto.RestPeriodThresholdInMinutes, updated.RestPeriodThresholdInMinutes);
        Assert.Equal(dto.MaximumConsecutiveWorkHoursPerWeek, updated.MaximumConsecutiveWorkHoursPerWeek);
    }

    [Fact]
    public async Task GetPolicyAsync_ReturnsMappedPolicyDto()
    {
        await using var dbContext = CreateDbContext();
        var entity = new WorkPolicy
        {
            MaximumConsecutiveWorkHoursPerDay = 9,
            RestPeriodInMinutes = 20,
            RestPeriodThresholdInMinutes = 330,
            MaximumConsecutiveWorkHoursPerWeek = 45
        };
        dbContext.WorkPolicies.Add(entity);
        await dbContext.SaveChangesAsync();

        var expected = new CompanyPolicyDto
        {
            MaximumConsecutiveWorkHoursPerDay = 9,
            RestPeriodInMinutes = 20,
            RestPeriodThresholdInMinutes = 330,
            MaximumConsecutiveWorkHoursPerWeek = 45
        };

        var mapperMock = new Mock<IMapper>();
        mapperMock
            .Setup(m => m.Map<CompanyPolicyDto>(It.IsAny<WorkPolicy>()))
            .Returns(expected);

        var service = new CompanyPolicyService(dbContext, mapperMock.Object);

        var result = await service.GetPolicyAsync();

        Assert.NotNull(result);
        Assert.Equal(expected.MaximumConsecutiveWorkHoursPerDay, result.MaximumConsecutiveWorkHoursPerDay);
        Assert.Equal(expected.RestPeriodInMinutes, result.RestPeriodInMinutes);
        Assert.Equal(expected.RestPeriodThresholdInMinutes, result.RestPeriodThresholdInMinutes);
        Assert.Equal(expected.MaximumConsecutiveWorkHoursPerWeek, result.MaximumConsecutiveWorkHoursPerWeek);
    }

    [Fact]
    public async Task GetPolicyAsync_WhenNoPolicyExists_ThrowsNotSetException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = new Mock<IMapper>();
        var service = new CompanyPolicyService(dbContext, mapperMock.Object);

        await Assert.ThrowsAsync<NotSetException>(() => service.GetPolicyAsync());
    }

    private static SchichtpilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SchichtpilotDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SchichtpilotDbContext(options);
    }
}
