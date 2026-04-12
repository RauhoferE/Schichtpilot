using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Responses;
namespace Schichtpilot.Interfaces;

public interface IAbsenceService
{
    // Employee UCs
    Task CreateAbsenceRequestAsync(long userId, CreateAbsenceDto dto); // UC-02-01-01
    Task DeleteOwnAbsenceAsync(int id, long userId); // UC-02-01-02
    Task<QueryableAbsenceResponse> ViewUserAbsencesAsync(PaginationDto pagination, AbsenceFilterDto? filter, long userId); // UC-02-01-04

    // Manager UCs  
    Task<QueryableAbsenceResponse> ViewAllAbsencesAsync(PaginationDto pagination, AbsenceFilterDto? filter); // UC-02-01-03
    Task<AbsenceDto> GetAbsenceDetailAsync(int id); // FR160
    Task UpdateAbsenceStatusAsync(int id, StatusUpdateDto dto);  // Single method for approve/deny

}