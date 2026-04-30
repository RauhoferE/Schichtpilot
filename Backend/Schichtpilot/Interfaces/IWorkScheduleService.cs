using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Interfaces;

/// <summary>
/// Orchestrates work schedule related operations, including generating, updating, publishing, deleting workschedules.
/// Also includes setting the work schedule as active/inactive.
/// </summary>
public interface IWorkScheduleService
{
    /// <summary>
    /// Generates a new work schedule from a shift.
    /// </summary>
    /// <param name="generateScheduleDto"> The parameters of the to be generated schedule. </param>
    /// <returns></returns>
    Task GenerateScheduleAsync(GenerateScheduleDto generateScheduleDto);

    /// <summary>
    /// Regenerates an existing schedule.
    /// Cannot be done on active schedule or deleted schedule 
    /// </summary>
    /// <param name="scheduleId"> The schedule to be regenerated. </param>
    /// <returns></returns>
    Task ReGenerateScheduleAsync(int scheduleId);

    /// <summary>
    /// Publishes an existing schedule.
    /// Can only be done on active schedule
    /// </summary>
    /// <param name="scheduleId"> The schedule to be published. </param>
    /// <returns></returns>
    Task PublishScheduleAsync(int scheduleId);

    /// <summary>
    /// Gets a list of existing schedules. 
    /// </summary>
    /// <param name="paginationDto"> The pagination element. </param>
    /// <param name="filter"> How to sort and filter the schedules. </param>
    /// <returns> Returns the schedules as <see cref="QueryableSchedules"/>. </returns>
    Task<QueryableSchedules> GetSchedulesAsync(PaginationDto paginationDto, ScheduleFilterDot? filter);

    /// <summary>
    /// Gets the workschedule for the start date.
    /// </summary>
    /// <param name="startDate"> The start date of the schedule. </param>
    /// <returns> Returns the work schedule as <see cref="WorkScheduleDto"/>. </returns>
    Task<WorkScheduleDto> GetActiveScheduleForDateAsync(DateTime startDate);

    // Cannot be done on active schedule or deleted schedule
    /// <summary>
    /// Gets the details of a workschedule.
    /// </summary>
    /// <param name="scheduleId"> The targeted schedule. </param>
    /// <returns> Returns the work schedule as <see cref="WorkScheduleDto"/>. </returns>
    Task<WorkScheduleDto> GetScheduleAsync(int scheduleId);

    /// <summary>
    /// Deletes an existing schedule.
    /// </summary>
    /// <param name="scheduleId"> The schedule to be deleted. </param>
    /// <returns></returns>
    Task DeleteScheduleAsync(int scheduleId);

    /// <summary>
    /// Sets an existing schedule as active.
    /// </summary>
    /// <param name="scheduleId"> The targeted schedule. </param>
    /// <returns></returns>
    Task SetScheduleActiveAsync(int scheduleId);

    /// <summary>
    /// Sets an existing schedule as inacitve.
    /// </summary>
    /// <param name="scheduleId"> The targeted schedule. </param>
    /// <returns></returns>
    Task SetScheduleOfflineAsync(int scheduleId);

    /// <summary>
    /// Sets an existing schedule as invalid.
    /// </summary>
    /// <param name="scheduleId"> The targetd schedule. </param>
    /// <returns></returns>
    Task SetScheduleAsInvalidAsync(int scheduleId);

    Task RemoveAllShiftAssignments(int scheduleId);

    /// <summary>
    /// Changes the start and end dates of a schedule.
    /// </summary>
    /// <param name="scheduleId"> The schedule to be updated. </param>
    /// <param name="startDate"> The new start date of the schedule. </param>
    /// <param name="endDate"> The end date of the schedule. </param>
    /// <returns></returns>
    Task ChangeScheduleDateAsync(int scheduleId, DateTime startDate, DateTime endDate);
}