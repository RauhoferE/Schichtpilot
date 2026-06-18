using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Responses;
namespace Schichtpilot.Interfaces;

/// <summary>
/// Orchestrates absence related operations, including creating, updating, deleting viewing user/all user absences.
/// </summary>
public interface IAbsenceService
{
    // Employee UCs

    /// <summary>
    /// Creates a new absence request.
    /// </summary>
    /// <param name="userId"> The user that created the absence. </param>
    /// <param name="dto"> The absence specifics. </param>
    /// <returns></returns>
    Task CreateAbsenceRequestAsync(long userId, CreateAbsenceDto dto); // UC-02-01-01

    /// <summary>
    /// Deletes an absence created by the user itself.
    /// </summary>
    /// <param name="id"> The absence to be deleted. </param>
    /// <param name="userId"> The user id that created the absence. </param>
    /// <returns></returns>
    Task DeleteOwnAbsenceAsync(int id, long userId); // UC-02-01-02

    /// <summary>
    /// Returns all user created absences.
    /// </summary>
    /// <param name="pagination"> The pagination element. </param>
    /// <param name="filter"> How to filter the available absences. </param>
    /// <param name="userId"> The user that created the absences. </param>
    /// <returns> Return the absences as <see cref="QueryableAbsenceResponse"/>. </returns>
    Task<QueryableAbsenceResponse> ViewUserAbsencesAsync(PaginationDto pagination, AbsenceFilterDto? filter, long userId); // UC-02-01-04

    // Manager UCs  

    /// <summary>
    /// Returns all created absences from all users.
    /// </summary>
    /// <param name="pagination"> The pagination element. </param>
    /// <param name="filter"> How to filter the available absences. </param>
    /// <returns> Return the absences as <see cref="QueryableAbsenceResponse"/>. </returns>
    Task<QueryableManagerAbsenceResponse> ViewAllAbsencesAsync(PaginationDto pagination, AbsenceFilterDto? filter); // UC-02-01-03

    /// <summary>
    /// Gets more details about a specific absence.
    /// </summary>
    /// <param name="id"> The id of the absence. </param>
    /// <returns> Returns the absence as a <see cref="AbsenceDto"/>. </returns>
    Task<AbsenceDto> GetAbsenceDetailAsync(int id); // FR160

    /// <summary>
    /// Updates a specific absence.
    /// </summary>
    /// <param name="id"> The id of the absence to update. </param>
    /// <param name="dto"> The new status for the absence. </param>
    /// <returns></returns>
    Task UpdateAbsenceStatusAsync(int id, StatusUpdateDto dto);  // Single method for approve/deny

}