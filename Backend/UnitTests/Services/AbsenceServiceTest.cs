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
using Schichtpilot.Services;

namespace UnitTests.Services;

[TestSubject(typeof(AbsenceService))]
public class AbsenceServiceTest
{
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    
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
                    //AbsenceType = e.AbsenceType,
                    Message = e.Message ?? "",
                    //Status = e.Status,
                    
                    CreatedAt = e.CreatedAt,
                    ManagerMessage = e.ManagerMessage ?? "",
                }).AsQueryable()
            );

        _emailServiceMock = new Mock<IEmailService>();
    }
    private SchichtpilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SchichtpilotDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new SchichtpilotDbContext(options);
    }

    private AbsenceService CreateService(SchichtpilotDbContext dbContext)
    {
        return new AbsenceService(dbContext, _mapperMock.Object, _emailServiceMock.Object);
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
            AbsenceType = "Vacation",
        };

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => service.CreateAbsenceRequestAsync(dto, 999));
    }

    [Fact]
    public async Task CreateAbsenceRequestAsync_InvalidPastDates_ThrowsValidationException()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateCompleteUser(1);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var dto = new CreateAbsenceDto
        {
            StartDate = DateTime.UtcNow.AddDays(-1).Date,
            EndDate = DateTime.UtcNow.Date,
            AbsenceType = "Vacation",
        };
        
        await Assert.ThrowsAsync<ValidationException>(
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
            Status = "Approved"
        };
        dbContext.Absences.Add(overlapping);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var dto = new CreateAbsenceDto
        {
            StartDate = DateTime.UtcNow.AddDays(3).Date,
            EndDate = DateTime.UtcNow.AddDays(5).Date,
            AbsenceType = "SickLeave"
        };

        await Assert.ThrowsAsync<AlreadyExistsException>(
            () => service.CreateAbsenceRequestAsync(dto, user.Id));
    }
    
    [Fact]
    public async Task CreateAbsenceRequestAsync_ValidDto_CreatesPendingAndSendsNotification()
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
            AbsenceType = "Vacation",
            Message = "Vacation request"
        };

        await service.CreateAbsenceRequestAsync(dto, user.Id);  

        var absence = await dbContext.Absences.FirstOrDefaultAsync();
        Assert.NotNull(absence);
        Assert.Equal("Pending", absence.Status);
        Assert.Equal(dto.StartDate, absence.StartDate);
        
        _emailServiceMock.Verify(x => x.SendNewAbsenceNotificationAsync(
            absence.Id, "John Doe"), Times.Once);
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
        dbContext.Absences.Add(new Absence { Id = 1, UserId = 1, Status = "Approved" });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.DeleteOwnAbsenceAsync(1, 1));
    }

    [Fact]
    public async Task DeleteOwnAbsenceAsync_OwnPendingAbsence_DeletesSuccessfully()
    {
        await using var dbContext = CreateDbContext();
        var absence = new Absence { Id = 1, UserId = 1, Status = "Pending" };
        dbContext.Absences.Add(absence);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await service.DeleteOwnAbsenceAsync(1, 1);

        var remaining = await dbContext.Absences.FindAsync(1);
        Assert.Null(remaining);
    }
       /** [Fact]
    public async Task ViewOwnAbsencesAsync_WithStatusFilter_ReturnsFilteredPagedResults()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateCompleteUser(1);
        dbContext.Users.Add(user);
        
        dbContext.Absences.AddRange(
            new Absence { UserId = 1, Status = "Approved", CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new Absence { UserId = 1, Status = "Pending", CreatedAt = DateTime.UtcNow.AddDays(-1), AbsenceType = "Vacation" }
        );
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var pagination = new PaginationDto { Page = 1, PageSize = 1 };
        var filter = new AbsenceFilterDto { Status = new List<AbsenceStatusEnum> {AbsenceStatusEnum.Pending} };

        var result = await service.ViewOwnAbsencesAsync(pagination, filter, 1);

        Assert.Equal(1, result.Count);
        Assert.Single(result.Absences);
        
        // 👈 Mapper verification (covers ProjectTo or Map calls)
        _mapperMock.Verify(x => x.ConfigurationProvider, Times.AtLeastOnce);
    }

    [Fact]
    public async Task ViewOwnAbsencesAsync_WithSearchAndDateFilter_ReturnsMatchingResults()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateCompleteUser(1);
        dbContext.Users.Add(user);
        
        dbContext.Absences.AddRange(
            new Absence { Id = 1, UserId = 1, Status = "Pending", AbsenceType = "Vacation", 
                         CreatedAt = DateTime.UtcNow.AddDays(-2), StartDate = DateTime.UtcNow.AddDays(1).Date },
            new Absence { Id = 2, UserId = 1, Status = "Approved", AbsenceType = "Sick", 
                         CreatedAt = DateTime.UtcNow.AddDays(-1), StartDate = DateTime.UtcNow.AddDays(5).Date }
        );
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var pagination = new PaginationDto { Page = 1, PageSize = 10 };
        var filter = new AbsenceFilterDto 
        { 
            Searchstring = "john",
            Status = new List<AbsenceStatusEnum> {AbsenceStatusEnum.Pending },
            StartDateFrom = DateTime.UtcNow.Date
        };

        var result = await service.ViewOwnAbsencesAsync(pagination, filter, 1);

        Assert.Single(result.Absences);
        Assert.Equal(1, result.Absences[0].Id);
    }

    [Fact]
    public async Task ViewAllAbsencesAsync_ReturnsAllWithPagination()
    {
        await using var dbContext = CreateDbContext();
        var user1 = CreateCompleteUser(1);
        var user2 = CreateCompleteUser(2);
        dbContext.Users.AddRange(user1, user2);
        
        dbContext.Absences.AddRange(
            new Absence { UserId = 1, Status = "Approved", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Absence { UserId = 2, Status = "Pending", CreatedAt = DateTime.UtcNow.AddDays(-1) }
        );
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var pagination = new PaginationDto { Page = 1, PageSize = 1 };

        var result = await service.ViewAllAbsencesAsync(pagination, null);

        Assert.Equal(2, result.Count);
        Assert.Single(result.Absences);
    }
**/
       
    [Fact]
    public async Task UpdateAbsenceStatusAsync_NotPending_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Absences.Add(new Absence { Id = 1, Status = "Approved" });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var dto = new StatusUpdateDto { Status = "Approved" };

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateAbsenceStatusAsync(1, dto));
    }

    [Fact]
    public async Task UpdateAbsenceStatusAsync_DeniedWithoutMessage_ThrowsValidationException()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Absences.Add(new Absence { Id = 1, Status = "Pending" });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var dto = new StatusUpdateDto { Status = "Denied" };

        await Assert.ThrowsAsync<ValidationException>(
            () => service.UpdateAbsenceStatusAsync(1, dto));
    }

    [Fact]
    public async Task UpdateAbsenceStatusAsync_ValidUpdate_SavesChanges()
    {
        await using var dbContext = CreateDbContext();
        var absence = new Absence { Id = 1, Status = "Pending" };
        dbContext.Absences.Add(absence);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var dto = new StatusUpdateDto { Status = "Approved", ManagerMessage = "Approved by manager" };

        await service.UpdateAbsenceStatusAsync(1, dto);

        var updated = await dbContext.Absences.FindAsync(1);
        Assert.Equal("Approved", updated.Status);
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
