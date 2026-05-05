using Schichtpilot.Exceptions;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Responses;

namespace IntegrationTests.Fakes;

public class ThrowingAbsenceService : IAbsenceService
{
    public Task CreateAbsenceRequestAsync(long userId, CreateAbsenceDto dto) =>
        throw new NotImplementedException();

    public Task DeleteOwnAbsenceAsync(int id, long userId) =>
        throw new NotImplementedException();

    public Task<QueryableAbsenceResponse> ViewUserAbsencesAsync(PaginationDto pagination, AbsenceFilterDto? filter, long userId) =>
        throw new NotImplementedException();

    public Task<QueryableAbsenceResponse> ViewAllAbsencesAsync(PaginationDto pagination, AbsenceFilterDto? filter) =>
        throw new NotImplementedException();

    public Task<AbsenceDto> GetAbsenceDetailAsync(int id) =>
        throw new NotFoundException($"Absence with id {id} was not found.");

    public Task UpdateAbsenceStatusAsync(int id, StatusUpdateDto dto) =>
        throw new NotImplementedException();
}
