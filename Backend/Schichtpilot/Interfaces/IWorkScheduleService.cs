using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Interfaces;

public interface IWorkScheduleService
{   
    // Check if Shift Timeslots dont interject
    // CHeck if any person is abscent
    // For each job check if there are multiple available users and check if one person wants to be abscent
    // Check if users can be assigned with max working hours
    // Throws special exception when generation is not possible
    Task GenerateSchedule(GenerateScheduleDto generateScheduleDto);
    
    // Cannot be done on active schedule or deleted schedule
    Task ReGenerateSchedule(int scheduleId);

    // Can only be done on active schedule
    Task PublishSchedule(int scheduleId);
    
    // Dont show deleted schedule
    Task<QueryableSchedules> ViewSchedules(PaginationDto paginationDto, ScheduleFilterDot? filter);
    
    // Cannot be done on active schedule or deleted schedule
    Task<WorkScheduleDto> ViewSchedule(int  scheduleId);

    Task DeleteSchedule(int scheduleId);
    
    Task SetScheduleActive(int scheduleId);
    
    Task SetScheduleOffline(int scheduleId);

    Task ChangeScheduleDate(int  scheduleId, DateTime startDate, DateTime endDate);
}