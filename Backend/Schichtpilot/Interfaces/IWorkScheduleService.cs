using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Interfaces;

public interface IWorkScheduleService
{   
    // Check if Shift Timeslots dont interject
    // CHeck if any person is abscent
    // For each job check if there are multiple available users and check if one person wants to be abscent
    // Check if users can be assigned with max working hours
    // Throws special exception when generation is not possible
    Task GenerateScheduleAsync(GenerateScheduleDto generateScheduleDto);
    
    // Cannot be done on active schedule or deleted schedule
    Task ReGenerateScheduleAsync(int scheduleId);

    // Can only be done on active schedule
    Task PublishScheduleAsync(int scheduleId);
    
    // Dont show deleted schedule
    Task<QueryableSchedules> ViewSchedulesAsync(PaginationDto paginationDto, ScheduleFilterDot? filter);
    
    // Cannot be done on active schedule or deleted schedule
    Task<WorkScheduleDto> ViewScheduleAsync(int  scheduleId);

    Task DeleteScheduleAsync(int scheduleId);
    
    Task SetScheduleActiveAsync(int scheduleId);
    
    Task SetScheduleOfflineAsync(int scheduleId);
    
    Task SetScheduleAsInvalidAsync(int scheduleId);
    
    Task RemoveAllShiftAssignments(int scheduleId);

    Task ChangeScheduleDateAsync(int  scheduleId, DateTime startDate, DateTime endDate);
}