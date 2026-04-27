using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Responses;

namespace IntegrationTests.Fakes;

public class FakeAbsenceService : IAbsenceService
{
    public long? LastCreateUserId { get; private set; }
    public CreateAbsenceDto? LastCreateDto { get; private set; }

    public int? LastDeleteAbsenceId { get; private set; }
    public long? LastDeleteUserId { get; private set; }

    public int? LastUpdateAbsenceId { get; private set; }
    public StatusUpdateDto? LastUpdateDto { get; private set; }

    public PaginationDto? LastUserAbsencesPagination { get; private set; }
    public AbsenceFilterDto? LastUserAbsencesFilter { get; private set; }
    public long? LastUserAbsencesUserId { get; private set; }

    public PaginationDto? LastAllAbsencesPagination { get; private set; }
    public AbsenceFilterDto? LastAllAbsencesFilter { get; private set; }

    public int? LastGetAbsenceId { get; private set; }

    public Task CreateAbsenceRequestAsync(long userId, CreateAbsenceDto dto)
    {
        LastCreateUserId = userId;
        LastCreateDto = dto;
        return Task.CompletedTask;
    }

    public Task DeleteOwnAbsenceAsync(int id, long userId)
    {
        LastDeleteAbsenceId = id;
        LastDeleteUserId = userId;
        return Task.CompletedTask;
    }

    public Task<QueryableAbsenceResponse> ViewUserAbsencesAsync(PaginationDto pagination, AbsenceFilterDto? filter, long userId)
    {
        LastUserAbsencesPagination = pagination;
        LastUserAbsencesFilter = filter;
        LastUserAbsencesUserId = userId;
        return Task.FromResult(new QueryableAbsenceResponse
        {
            Count = 0,
            Absences = new List<AbsenceDto>()
        });
    }

    public Task<QueryableAbsenceResponse> ViewAllAbsencesAsync(PaginationDto pagination, AbsenceFilterDto? filter)
    {
        LastAllAbsencesPagination = pagination;
        LastAllAbsencesFilter = filter;
        return Task.FromResult(new QueryableAbsenceResponse
        {
            Count = 0,
            Absences = new List<AbsenceDto>()
        });
    }

    public Task<AbsenceDto> GetAbsenceDetailAsync(int id)
    {
        LastGetAbsenceId = id;
        return Task.FromResult(new AbsenceDto
        {
            Id = id,
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddDays(2),
            AbsenceType = Schichtpilot.Models.Enums.AbsenceTypeEnum.Vacation,
            Status = Schichtpilot.Models.Enums.AbsenceStatusEnum.Approved
        });
    }

    public Task UpdateAbsenceStatusAsync(int id, StatusUpdateDto dto)
    {
        LastUpdateAbsenceId = id;
        LastUpdateDto = dto;
        return Task.CompletedTask;
    }
}
