using AutoMapper;
using Data;
using Data.Entities;
using Microsoft.EntityFrameworkCore;
using Schichtpilot.Exceptions;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Enums;

namespace Schichtpilot.Services;

public class WorkScheduleService : IWorkScheduleService
{
    private readonly SchichtpilotDbContext _dbContext;
    
    private readonly IMapper _mapper;

    public WorkScheduleService(SchichtpilotDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task GenerateScheduleAsync(GenerateScheduleDto generateScheduleDto)
    {
        var shifts = await _dbContext.Shifts
            .Include(s => s.Timeslots)
            .Include(s => s.JobRequirements)
            .Where(s => generateScheduleDto.ShiftIds.Contains(s.Id))
            .ToListAsync();

        if (shifts.Count != generateScheduleDto.ShiftIds.Count)
        {
            throw new NotFoundException("One or more shifts were not found.");
        }
        
        var schedule = new WorkSchedule
        {
            Name = generateScheduleDto.Name,
            StartDate = generateScheduleDto.StartDate.Date,
            EndDate = generateScheduleDto.EndDate.Date,
            IsActive = false,
            IsValid = false,
            ShiftAssignments = new HashSet<ShiftAssignment>(),
            Shifts = new HashSet<WorkScheduleShifts>()
        };
        
        foreach (var shift in shifts)
        {
            schedule.Shifts.Add(new WorkScheduleShifts
            {
                ShiftId = shift.Id,
                WorkSchedule = schedule
            });
        }
        
        this._dbContext.WorkSchedules.Add(schedule);
        await this._dbContext.SaveChangesAsync();
        
        await CreateShiftAssignmentsAsync(schedule, shifts);
    }

    private async Task CreateShiftAssignmentsAsync(WorkSchedule schedule, List<Shift> shifts)
    {
        var endDateOfSchedule = schedule.EndDate;
        var startDateOfSchedule = schedule.StartDate;

        var allTimeslots = shifts
            .SelectMany(s => s.Timeslots)
            .OrderBy(t => t.DayOfWeek)
            .ThenBy(t => t.StartTime)
            .ToList();

        if (HasIntersections(allTimeslots))
        {
            throw new PolicyConflictException("Shifts have intersections with each other.");
        }
        
        var workPolicy = await _dbContext.WorkPolicies.FirstOrDefaultAsync();
        if (workPolicy == null)
        {
            throw new PolicyConflictException("WorkPolicy not configured.");
        }

        var requiredJobRoleIds = shifts
            .SelectMany(s => s.JobRequirements)
            .Select(r => r.JobRoleId)
            .Distinct()
            .ToList();

        var usersByRole = await _dbContext.UserJobRoles
            .Include(ujr => ujr.User)
            .Where(ujr => requiredJobRoleIds.Contains(ujr.JobRoleId))
            .OrderBy(ujr => ujr.UserId) // first-fit deterministic
            .ToListAsync();

        var approvedAbsences = await _dbContext.Absences
            .Where(a =>
                a.Status == AbsenceStatusEnum.Approved.ToString() &&
                a.StartDate < endDateOfSchedule.AddDays(1) &&
                startDateOfSchedule < a.EndDate)
            .ToListAsync();

        var pendingAbsences = await _dbContext.Absences
            .Where(a =>
                a.Status == AbsenceStatusEnum.Pending.ToString() &&
                a.StartDate < endDateOfSchedule.AddDays(1) &&
                startDateOfSchedule < a.EndDate)
            .ToListAsync();

        var assignmentsByUser = new Dictionary<long, List<(DateTime Start, DateTime End)>>();

        var timeslotsWithShift = shifts
            .SelectMany(s => s.Timeslots.Select(t => new { Shift = s, Timeslot = t }))
            .OrderBy(x => x.Timeslot.DayOfWeek)
            .ThenBy(x => x.Timeslot.StartTime)
            .ToList();

        foreach (var item in timeslotsWithShift)
        {
            var timeslot = item.Timeslot;
            var shift = item.Shift;

            var timeSlotBaseDate = startDateOfSchedule.Date
                .AddDays(((int)timeslot.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7);

            var slotStart = timeSlotBaseDate.Add(timeslot.StartTime.ToTimeSpan());
            var slotEnd = timeSlotBaseDate.Add(timeslot.EndTime.ToTimeSpan());

            // If there is a holiday when the timeslot starts or stops the shift is canceled 
            if (this._dbContext.Holidays.Any(x => slotStart.Date == x.Date.Date || slotEnd.Date == x.Date.Date))
            {
                continue;
            }

            foreach (var shiftRequirement in shift.JobRequirements)
            {
                // Canditates with pending absence will be picked last
                var candidates = usersByRole
                    .Where(u => u.JobRoleId == shiftRequirement.JobRoleId)
                    .Select(u => new
                    {
                        UserJobRole = u,
                        HasPendingOverlap = pendingAbsences.Any(a =>
                            a.UserId == u.UserId &&
                            a.StartDate < slotEnd &&
                            slotStart < a.EndDate)
                    })
                    .OrderBy(x => x.HasPendingOverlap) // false first => no pending absence first
                    .ThenBy(x => x.UserJobRole.UserId) // first-fit deterministic
                    .Select(x => x.UserJobRole)
                    .ToList();

                var assignedCount = 0;

                foreach (var candidate in candidates)
                {
                    if (assignedCount >= shiftRequirement.RequiredStaffCount)
                    {
                        break;
                    }
                    
                    // Approved absences still block assignment
                    var candidateApprovedAbsenceOverlap = approvedAbsences.Any(a =>
                        a.UserId == candidate.UserId &&
                        a.StartDate < slotEnd &&
                        slotStart < a.EndDate);

                    if (candidateApprovedAbsenceOverlap)
                    {
                        continue;
                    }

                    if (!assignmentsByUser.TryGetValue(candidate.UserId, out var userAssignments))
                    {
                        userAssignments = new List<(DateTime Start, DateTime End)>();
                        assignmentsByUser[candidate.UserId] = userAssignments;
                    }

                    var overlapsExisting = userAssignments.Any(x =>
                        x.Start < slotEnd && slotStart < x.End);

                    if (overlapsExisting)
                    {
                        continue;
                    }
                    
                    if (!RespectsMaximumConsecutiveHours(userAssignments, slotStart, slotEnd,
                            workPolicy.MaximumConsecutiveWorkHoursPerDay))
                    {
                        continue;
                    }
                    
                    if (!RespectsMaximumWeeklyHours(
                            userAssignments,
                            slotStart,
                            slotEnd,
                            workPolicy.MaximumConsecutiveWorkHoursPerWeek))
                    {
                        continue;
                    }

                    schedule.ShiftAssignments.Add(new ShiftAssignment
                    {
                        WorkSchedule = schedule,
                        TimeslotId = timeslot.Id,
                        UserId = candidate.UserId,
                        JobRoleId = candidate.JobRoleId,
                        StartTime = slotStart,
                        EndTime = slotEnd
                    });

                    userAssignments.Add((slotStart, slotEnd));
                    assignedCount++;
                }

                if (assignedCount < shiftRequirement.RequiredStaffCount)
                {
                    throw new PolicyConflictException(
                        $"Not enough staff for ShiftId={shift.Id}, TimeslotId={timeslot.Id}, JobRoleId={shiftRequirement.JobRoleId}.");
                }
            }
        }

        schedule.IsValid = true;
        _dbContext.WorkSchedules.Update(schedule);
        await _dbContext.SaveChangesAsync();
    }

    private bool RespectsMaximumConsecutiveHours(
        List<(DateTime Start, DateTime End)> userAssignments,
        DateTime newTimeslotStart,
        DateTime newTimeslotEnd,
        int maximumConsecutiveWorkHours)
    {
        var assignments = new List<(DateTime Start, DateTime End)>(userAssignments)
        {
            (newTimeslotStart, newTimeslotEnd)
        };

        assignments = assignments.OrderBy(x => x.Start).ToList();

        var currentConsecutiveHoursOfUser = TimeSpan.Zero;
        var currentChainStart = assignments[0].Start;
        var currentChainEnd = assignments[0].End;

        for (var i = 1; i < assignments.Count; i++)
        {
            var next = assignments[i];

            if (next.Start <= currentChainEnd)
            {
                if (next.End > currentChainEnd)
                    currentChainEnd = next.End;
            }
            else
            {
                var segment = currentChainEnd - currentChainStart;
                if (segment > currentConsecutiveHoursOfUser)
                {
                    currentConsecutiveHoursOfUser = segment;
                }

                currentChainStart = next.Start;
                currentChainEnd = next.End;
            }
        }

        var finalSegment = currentChainEnd - currentChainStart;
        if (finalSegment > currentConsecutiveHoursOfUser)
        {
            currentConsecutiveHoursOfUser = finalSegment;
        }

        return currentConsecutiveHoursOfUser <= TimeSpan.FromHours(maximumConsecutiveWorkHours);
    }
    
    private bool RespectsMaximumWeeklyHours(
        List<(DateTime Start, DateTime End)> userAssignments,
        DateTime newTimeslotStart,
        DateTime newTimeslotEnd,
        int maximumWeeklyHours)
    {
        TimeSpan total = TimeSpan.Zero;

        foreach (var assignment in userAssignments)
        {
            total += assignment.End - assignment.Start;
        }
        
        total += newTimeslotEnd - newTimeslotStart;

        return total <= TimeSpan.FromHours(maximumWeeklyHours);
    }
    
    private bool HasIntersections(List<Timeslot> slots)
    {
        if (slots == null || slots.Count < 2) return false;

        var groupedByDayOfWeek = slots.GroupBy(s => s.DayOfWeek);

        foreach (var daySlots in groupedByDayOfWeek)
        {
            var daysOrderedAfterStartTime = daySlots
                .OrderBy(s => s.StartTime)
                .ToList();

            for (int i = 0; i < daysOrderedAfterStartTime.Count - 1; i++)
            {
                if (daysOrderedAfterStartTime[i + 1].StartTime < daysOrderedAfterStartTime[i].EndTime)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public async Task ReGenerateScheduleAsync(int scheduleId)
    {
        var schedule = this._dbContext.WorkSchedules
            .Include(x => x.Shifts)
            .ThenInclude(x => x.Shift)
            .ThenInclude(x => x.Timeslots)
            .Include(x => x.Shifts)
            .ThenInclude(x => x.Shift)
            .ThenInclude(x => x.JobRequirements)
            .Include(x => x.ShiftAssignments)
            .FirstOrDefault(x => x.Id == scheduleId);
        

        if (schedule == null)
        {
            throw new NotFoundException($"Schedule with id {scheduleId} not found.");
        }

        var shifts = schedule.Shifts.Select(x => x.Shift).ToList();
        
        // Set to inactive since we dont know if generation is possible
        schedule.IsActive = false;
        schedule.IsValid = false;
        
        // Remove all previous assignments
        await RemoveAllShiftAssignments(schedule);
        
        await CreateShiftAssignmentsAsync(schedule, shifts);
    }

    private async Task RemoveAllShiftAssignments(WorkSchedule schedule)
    {
        foreach (var shiftAssignment in schedule.ShiftAssignments)
        {
            this._dbContext.ShiftAssignments.Remove(shiftAssignment);
        }

        await this._dbContext.SaveChangesAsync();
    }

    public async Task PublishScheduleAsync(int scheduleId)
    {
        //TODO: Send via emailservice
        throw new NotImplementedException();
    }

    public async Task<QueryableSchedules> GetSchedulesAsync(PaginationDto paginationDto, ScheduleFilterDot? filter)
    {
        IQueryable<WorkSchedule> query = this._dbContext.WorkSchedules
            .Include(x => x.Shifts)
            .ThenInclude(x => x.Shift)
            .AsQueryable();
        
        if (filter != null)
        {
            query = await this.FilterSchedulesAsync(query, filter);
        }
        
        return new QueryableSchedules()
        {
            Count = query.Count(),
            WorkSchedules = query
                .Skip((paginationDto.Page - 1) * paginationDto.PageSize)
                .Take(paginationDto.PageSize)
                .Select(x => this._mapper.Map<WorkSchedule, WorkScheduleShortDto>(x))
        };
    }

    private async Task<IQueryable<WorkSchedule>> FilterSchedulesAsync(IQueryable<WorkSchedule> query, ScheduleFilterDot filter)
    {
        if (!string.IsNullOrEmpty(filter.Searchstring))
        {
            query = query.Where(x => x.Name.ToLower().Contains(filter.Searchstring.ToLower()));
        }

        if (filter.StartDate != null)
        {
            query = query.Where(x => x.StartDate >= filter.StartDate);
        }

        if (filter.EndDate != null)
        {
            query = query.Where(x => x.StartDate <= filter.EndDate);
        }

        switch (filter.Status)  
        {
            case ScheduleStatusEnum.All:
                break;
            case ScheduleStatusEnum.Active:
                query = query.Where(x => x.IsActive);
                break;
            case ScheduleStatusEnum.Inactive:
                query = query.Where(x => !x.IsActive);
                break;
            case ScheduleStatusEnum.Valid:
                query = query.Where(x => x.IsValid);
                break;
            case ScheduleStatusEnum.Invalid:
                query = query.Where(x => !x.IsValid);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (filter.ShiftIds.Count > 0)
        {
            query = query.Where(x => x.Shifts.Any(y => filter.ShiftIds.Contains(y.ShiftId)));
        }
        
        return query;
    }

    public async Task<WorkScheduleDto> GetScheduleAsync(int scheduleId)
    {
        var schedule = this._dbContext.WorkSchedules
            .Include(x => x.Shifts)
            .ThenInclude(x => x.Shift)
            .Include(x => x.ShiftAssignments)
            .ThenInclude(x => x.Timeslot)
            .ThenInclude(x => x.Breaks)
            .Include(x => x.ShiftAssignments)
            .ThenInclude(x => x.UserJobRole)
            .ThenInclude(x => x.JobRole)
            .FirstOrDefault(x => x.Id == scheduleId);

        if (schedule == null)
        {
            throw new NotFoundException($"Schedule with id {scheduleId} not found.");
        }

        return this._mapper.Map<WorkSchedule, WorkScheduleDto>(schedule);
    }

    public async Task DeleteScheduleAsync(int scheduleId)
    {
        var schedule = this._dbContext.WorkSchedules
            .Include(x => x.Shifts)
            .Include(x => x.ShiftAssignments)
            .FirstOrDefault(x => x.Id == scheduleId);

        if (schedule == null)
        {
            throw new NotFoundException($"Schedule with id {scheduleId} not found.");
        }

        if (schedule.IsActive)
        {
            throw new PolicyConflictException("Cannot delete active schedule");
        }
        
        this._dbContext.WorkSchedules.Remove(schedule);
        await this._dbContext.SaveChangesAsync();
    }

    public async Task SetScheduleActiveAsync(int scheduleId)
    {
        var schedule = this._dbContext.WorkSchedules
            .Include(x => x.Shifts)
            .Include(x => x.ShiftAssignments)
            .FirstOrDefault(x => x.Id == scheduleId);

        if (schedule == null)
        {
            throw new NotFoundException($"Schedule with id {scheduleId} not found.");
        }

        if (!schedule.IsValid)
        {
            throw new PolicyConflictException($"Schedule with id {scheduleId} is invalid.");
        }
        
        var overlappingSchedule = this._dbContext.WorkSchedules
            .FirstOrDefault(x => schedule.StartDate < x.EndDate && x.StartDate < schedule.EndDate 
                                                                && schedule.Id != x.Id && schedule.IsActive);

        if (overlappingSchedule != null)
        {
            throw new InvalidDependencyException($"Schedule is overlapping with another active schedule {overlappingSchedule.Name}.");
        }
        
        schedule.IsActive = true;
        await this._dbContext.SaveChangesAsync();
    }

    public async Task SetScheduleOfflineAsync(int scheduleId)
    {
        var schedule = this._dbContext.WorkSchedules
            .FirstOrDefault(x => x.Id == scheduleId);

        if (schedule == null)
        {
            throw new NotFoundException($"Schedule with id {scheduleId} not found.");
        }
        // TODO: Send an email to managers that schedule has been set as offline
        schedule.IsActive = false;
        await this._dbContext.SaveChangesAsync();
    }

    public async Task SetScheduleAsInvalidAsync(int scheduleId)
    {
        var schedule = this._dbContext.WorkSchedules
            .FirstOrDefault(x => x.Id == scheduleId);

        if (schedule == null)
        {
            throw new NotFoundException($"Schedule with id {scheduleId} not found.");
        }
        
        schedule.IsValid = false;
        await this._dbContext.SaveChangesAsync();
    }

    public async Task RemoveAllShiftAssignments(int scheduleId)
    {
        var schedule = this._dbContext.WorkSchedules
            .Include(x => x.ShiftAssignments)
            .FirstOrDefault(x => x.Id == scheduleId);
        

        if (schedule == null)
        {
            throw new NotFoundException($"Schedule with id {scheduleId} not found.");
        }

        await this.RemoveAllShiftAssignments(schedule);
    }

    public async Task ChangeScheduleDateAsync(int scheduleId, DateTime startDate, DateTime endDate)
    {
        var schedule = this._dbContext.WorkSchedules
            .FirstOrDefault(x => x.Id == scheduleId);

        if (schedule == null)
        {
            throw new NotFoundException($"Schedule with id {scheduleId} not found.");
        }
        
        schedule.StartDate = startDate;
        schedule.EndDate = endDate;
        await this._dbContext.SaveChangesAsync();
        await this.ReGenerateScheduleAsync(schedule.Id);
    }
}