using Data;
using Data.Entities;
using Microsoft.EntityFrameworkCore;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Enums;

namespace Schichtpilot.Interfaces;

public class GenerateScheduleDto
{
    public required string Name { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public required List<int> ShiftIds { get; set; }
}

public interface IWorkScheduleService
{   
    // Check if Shift Timeslots dont interject
    // CHeck if any person is abscent
    // For each job check if there are multiple available users and check if one person wants to be abscent
    // Check if users can be assigned with max working hours
    // Throws special exception when generation is not possible
    Task GenerateSchedule(GenerateScheduleDto generateScheduleDto);

    Task ReGenerateSchedule(int scheduleId);

    Task PublishSchedule(int scheduleId);
    
    Task ViewSchedules(PaginationDto paginationDto);
    
    Task ViewSchedule(int  scheduleId);

    Task DeleteSchedule(int scheduleId);
    
    Task SetScheduleActive(int scheduleId);
    
    Task SetScheduleOffline(int scheduleId);

    Task CloneSchedule(int scheduleId, DateTime startDate, DateTime endDate);

    Task ChangeScheduleDate(int  scheduleId, DateTime startDate, DateTime endDate);
}

public class WorkScheduleService : IWorkScheduleService
{
    private readonly SchichtpilotDbContext _dbContext;

    public WorkScheduleService(SchichtpilotDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task GenerateSchedule(GenerateScheduleDto generateScheduleDto)
    {
        var timeSlotsInSchedule = this._dbContext.Timeslots
            .Where(x => generateScheduleDto.ShiftIds.Contains(x.ShiftId))
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToList();

        if (this.HasIntersections(timeSlotsInSchedule))
        {
            //TODO: Change to custom
            throw new Exception("Shifts have intersect with each other");
        }

        var shiftRequirements = this._dbContext.ShiftRequirements
            .Include(x => x.JobRole)
            .Where(x => generateScheduleDto.ShiftIds.Contains(x.ShiftId))
            .ToList();

        foreach (var shiftRequirement in shiftRequirements)
        {
            List<UserJobRoles> usersForJob = new List<UserJobRoles>();
            var possibleUsersForTheJob =
                this._dbContext.UserJobRoles
                    .Include(x => x.JobRole)
                    .Include(x => x.User)
                    .Where(x => x.JobRoleId == shiftRequirement.JobRoleId);

            // CHeck for abscences
            foreach (var user in possibleUsersForTheJob)
            {
                var isAbscent = this._dbContext.Absences
                    .Any(x => x.UserId == user.UserId &&
                                x.StartDate < generateScheduleDto.EndDate && generateScheduleDto.StartDate < x.EndDate
                                && x.Status == AbsenceStatusEnum.Approved.ToString());

                if (!isAbscent)
                {
                    usersForJob.Add(user);
                }
                
            }
            
            // Check if there is enough staff
            if (shiftRequirement.RequiredStaffCount > usersForJob.Count)
            {
                throw new Exception("Not enough staff");
            }
        }
        
        
    }
    
    private bool HasIntersections(List<Timeslot> slots)
    {
        if (slots == null || slots.Count < 2) return false;

        // 2. Compare each slot with the one immediately following it
        for (int i = 0; i < slots.Count - 1; i++)
        {
            var current = slots[i];
            var next = slots[i + 1];

            // If they are on the same day, check for time overlap
            if (current.DayOfWeek == next.DayOfWeek && next.StartTime < current.EndTime)
            {
                return true;
            }
        }

        return false;
    }

    public async Task ReGenerateSchedule(int scheduleId)
    {
        throw new NotImplementedException();
    }

    public async Task PublishSchedule(int scheduleId)
    {
        throw new NotImplementedException();
    }

    public async Task ViewSchedules(PaginationDto paginationDto)
    {
        throw new NotImplementedException();
    }

    public async Task ViewSchedule(int scheduleId)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteSchedule(int scheduleId)
    {
        throw new NotImplementedException();
    }

    public async Task SetScheduleActive(int scheduleId)
    {
        throw new NotImplementedException();
    }

    public async Task SetScheduleOffline(int scheduleId)
    {
        throw new NotImplementedException();
    }

    public async Task CloneSchedule(int scheduleId, DateTime startDate, DateTime endDate)
    {
        throw new NotImplementedException();
    }

    public async Task ChangeScheduleDate(int scheduleId, DateTime startDate, DateTime endDate)
    {
        throw new NotImplementedException();
    }
}