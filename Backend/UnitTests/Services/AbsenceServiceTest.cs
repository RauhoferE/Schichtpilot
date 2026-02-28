using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Data;
using Data.Entities;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Schichtpilot.Exceptions;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Enums;
using Schichtpilot.Models.Responses;
using Schichtpilot.Services;
using Xunit;

namespace UnitTests.Services;

[TestSubject(typeof(AbsenceService))]
public class AbsenceServiceTest
{
    private readonly IMapper _mapper;
    private readonly Mock<IEmailService> _emailServiceMock;

    public AbsenceServiceTest()
    {
        var config = new MapperConfiguration(cfg => cfg.CreateMap<Absence, AbsenceDto>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

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

    [Fact]
    public async Task CreateAbsenceRequestAsync_UserNotFound_ThrowsUserNotFoundException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var dto = new CreateAbsenceDto
        {
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddDays(2),
            AbsenceType = "Vacation",
        };

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(
            () => service.CreateAbsenceRequestAsync(dto, 999));
    }

    [Fact]
    public async Task CreateAbsenceRequestAsync_InvalidPastDates_ThrowsValidationException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new User { Id = 1, FirstName = "Test", LastName = "User" });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var dto = new CreateAbsenceDto
        {
            StartDate = DateTime.UtcNow.Date.AddDays(-1), // Past
            EndDate = DateTime.UtcNow.Date.AddDays(-2),   // End < Start
            AbsenceType = "Vacation",
        };

        // Act & Assert
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

/**
    [Fact]
    public async Task CreateAbsenceRequestAsync_ValidDto_CreatesPendingAndSendsNotification()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var dto = new CreateAbsenceDto
        {
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddDays(2),
            AbsenceType = "Vacation",
            Message = "Vacation request"
        };

        // Act
        await service.CreateAbsenceRequestAsync(dto, 1);

        // Assert
        var absence = await dbContext.Absences.FirstOrDefaultAsync();
        Assert.NotNull(absence);
        Assert.Equal(1, absence.UserId);
        Assert.Equal(dto.StartDate.Date, absence.StartDate);
        Assert.Equal("Vacation", absence.AbsenceType);
        Assert.Equal("Pending", absence.Status);
        Assert.Equal("Vacation request", absence.Message);

        _emailServiceMock.Verify(x => x.SendNewAbsenceNotificationAsync(
            absence.Id, "John Doe"), Times.Once);
    }
**/
    
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
        Assert.Equal(user.Id, absence.UserId);  
        Assert.Equal("Pending", absence.Status);
        _emailServiceMock.Verify(x => x.SendNewAbsenceNotificationAsync(
            absence.Id, "John Doe"), Times.Once);
    }


    
    [Fact]
    public async Task DeleteOwnAbsenceAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.DeleteOwnAbsenceAsync(999, 1));
    }

    [Fact]
    public async Task DeleteOwnAbsenceAsync_ApprovedAbsence_ThrowsNotFoundException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        dbContext.Absences.Add(new Absence { Id = 1, UserId = 1, Status = "Approved" });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.DeleteOwnAbsenceAsync(1, 1));
    }

    [Fact]
    public async Task DeleteOwnAbsenceAsync_OwnPendingAbsence_DeletesSuccessfully()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var absence = new Absence { Id = 1, UserId = 1, Status = "Pending" };
        dbContext.Absences.Add(absence);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        await service.DeleteOwnAbsenceAsync(1, 1);

        // Assert
        var remaining = await dbContext.Absences.FindAsync(1);
        Assert.Null(remaining);
    }

    [Fact]
    public async Task ViewOwnAbsencesAsync_WithStatusFilter_ReturnsFilteredPagedResults()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@test.com" };
        dbContext.Users.Add(user);
        
        dbContext.Absences.AddRange(
            new Absence { UserId = 1, Status = "Approved", CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new Absence { UserId = 1, Status = "Pending", CreatedAt = DateTime.UtcNow.AddDays(-1), AbsenceType = "Vacation" }
        );
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var pagination = new PaginationDto { Page = 1, PageSize = 1 };
        var filter = new AbsenceFilterDto 
        { 
            Status = new List<AbsenceStatusEnum> { AbsenceStatusEnum.Pending } 
        };

        // Act
        var result = await service.ViewOwnAbsencesAsync(pagination, filter, 1);

        // Assert
        Assert.Equal(1, result.Count);
        Assert.Single(result.Absences);
    }

    [Fact]
    public async Task ViewOwnAbsencesAsync_WithSearchAndDateFilter_ReturnsMatchingResults()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@test.com" };
        dbContext.Users.Add(user);
        
        dbContext.Absences.AddRange(
            new Absence { 
                Id = 1, 
                UserId = 1, 
                Status = "Pending", 
                AbsenceType = "Vacation", 
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                StartDate = DateTime.UtcNow.Date.AddDays(1)
            },
            new Absence { 
                Id = 2, 
                UserId = 1, 
                Status = "Approved", 
                AbsenceType = "Sick", 
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                StartDate = DateTime.UtcNow.Date.AddDays(5)
            }
        );
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var pagination = new PaginationDto { Page = 1, PageSize = 10 };
        var filter = new AbsenceFilterDto 
        { 
            Searchstring = "john",
            Status = new List<AbsenceStatusEnum> { AbsenceStatusEnum.Pending },
            StartDateFrom = DateTime.UtcNow.Date
        };

        // Act
        var result = await service.ViewOwnAbsencesAsync(pagination, filter, 1);

        // Assert
        Assert.Single(result.Absences);
    }

    [Fact]
    public async Task ViewAllAbsencesAsync_ReturnsAllWithPagination()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var user1 = new User { Id = 1, FirstName = "John", LastName = "Doe" };
        var user2 = new User { Id = 2, FirstName = "Jane", LastName = "Smith" };
        dbContext.Users.AddRange(user1, user2);
        
        dbContext.Absences.AddRange(
            new Absence { UserId = 1, Status = "Approved", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Absence { UserId = 2, Status = "Pending", CreatedAt = DateTime.UtcNow.AddDays(-1) }
        );
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var pagination = new PaginationDto { Page = 1, PageSize = 1 };

        // Act
        var result = await service.ViewAllAbsencesAsync(pagination, null);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Single(result.Absences);
    }

    [Fact]
    public async Task UpdateAbsenceStatusAsync_NotPending_ThrowsNotFoundException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        dbContext.Absences.Add(new Absence { Id = 1, Status = "Approved" });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var dto = new StatusUpdateDto { Status = "Approved" };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateAbsenceStatusAsync(1, dto));
    }

    [Fact]
    public async Task UpdateAbsenceStatusAsync_DeniedWithoutMessage_ThrowsValidationException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        dbContext.Absences.Add(new Absence { Id = 1, Status = "Pending" });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var dto = new StatusUpdateDto { Status = "Denied" }; // No message

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => service.UpdateAbsenceStatusAsync(1, dto));
    }

    [Fact]
    public async Task UpdateAbsenceStatusAsync_ValidUpdate_SavesChanges()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var absence = new Absence { Id = 1, Status = "Pending" };
        dbContext.Absences.Add(absence);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var dto = new StatusUpdateDto 
        { 
            Status = "Approved",
            ManagerMessage = "Approved by manager"
        };

        // Act
        await service.UpdateAbsenceStatusAsync(1, dto);

        // Assert
        var updated = await dbContext.Absences.FindAsync(1);
        Assert.Equal("Approved", updated.Status);
        Assert.Equal("Approved by manager", updated.ManagerMessage);
    }

    [Fact]
    public async Task GetAbsenceDetailAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetAbsenceDetailAsync(999));
    }

    private AbsenceService CreateService(SchichtpilotDbContext dbContext)
    {
        return new AbsenceService(dbContext, _mapper, _emailServiceMock.Object);
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
            City = "Testville",           // 👈 REQUIRED
            StreetAddress = "Main St 1",  // 👈 REQUIRED
            PostalCode = 12345,
            EmailConfirmed = true
        };
    }
}
