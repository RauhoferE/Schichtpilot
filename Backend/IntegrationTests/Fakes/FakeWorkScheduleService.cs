using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;


namespace IntegrationTests.Fakes;

public class FakeWorkScheduleService : IWorkScheduleService
{
    public int? LastScheduleId { get; private set; }
    public DateTime? LastStartDate { get; private set; }
    public DateTime? LastEndDate { get; private set; }
    public int ChangeScheduleDateCallCount { get; private set; }

    public Task GenerateScheduleAsync(GenerateScheduleDto generateScheduleDto)
    {
        return Task.CompletedTask;
    }

    public Task ReGenerateScheduleAsync(int scheduleId)
    {
        return Task.CompletedTask;
    }

    public Task PublishScheduleAsync(int scheduleId)
    {
        return Task.CompletedTask;
    }

    public Task<QueryableSchedules> GetSchedulesAsync(PaginationDto paginationDto, ScheduleFilterDot? filter)
    {
        return Task.FromResult(new QueryableSchedules
        {
            Count = 0,
            WorkSchedules = new List<WorkScheduleShortDto>()
        });
    }

    public Task<WorkScheduleDto> GetActiveScheduleForDateAsync(DateTime startDate)
    {
        return Task.FromResult(new WorkScheduleDto
        {
            Id = 1,
            Name = "Integration Test Schedule",
            StartDate = startDate.Date,
            EndDate = startDate.Date.AddDays(6),
            IsActive = true,
            IsValid = true,
            AssignedUsers = new List<AssignedUserDto>(),
            Shifts = new List<ShiftDto>()
        });
    }

    public Task<WorkScheduleDto> GetScheduleAsync(int scheduleId)
    {
        return Task.FromResult(new WorkScheduleDto
        {
            Id = scheduleId,
            Name = "Integration Test Schedule",
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddDays(7),
            IsActive = false,
            IsValid = true,
            AssignedUsers = new List<AssignedUserDto>(),
            Shifts = new List<ShiftDto>()
        });
    }

    public Task DeleteScheduleAsync(int scheduleId)
    {
        return Task.CompletedTask;
    }

    public Task SetScheduleActiveAsync(int scheduleId)
    {
        return Task.CompletedTask;
    }

    public Task SetScheduleOfflineAsync(int scheduleId)
    {
        return Task.CompletedTask;
    }

    public Task SetScheduleAsInvalidAsync(int scheduleId)
    {
        return Task.CompletedTask;
    }

    public Task RemoveAllShiftAssignments(int scheduleId)
    {
        return Task.CompletedTask;
    }

    public Task ChangeScheduleDateAsync(int scheduleId, DateTime startDate, DateTime endDate)
    {
        ChangeScheduleDateCallCount++;
        LastScheduleId = scheduleId;
        LastStartDate = startDate;
        LastEndDate = endDate;
        return Task.CompletedTask;
    }
}
