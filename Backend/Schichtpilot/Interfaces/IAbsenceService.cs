using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Interfaces;

using Data.Entities;


public interface IAbsenceService
{
    Task<long> CreateAbsenceRequestAsync(CreateAbsenceDto dto, long userId);
    Task<List<Absence>> GetMyAbsenceRequestsAsync(long userId);
    Task DeleteMyAbsenceRequestAsync(long id, long userId);
    Task<List<Absence>> GetAllAbsenceRequestsAsync(AbsenceFilterDto filter); // Sorting here!
    Task UpdateStatusAsync(long id, StatusUpdateDto dto);
}
