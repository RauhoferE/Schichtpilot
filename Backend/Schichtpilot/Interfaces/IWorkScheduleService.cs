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
    
    // Cannot be done on active schedule or deleted schedule
    Task ReGenerateSchedule(int scheduleId);

    // Can only be done on active schedule
    Task PublishSchedule(int scheduleId);
    
    // Dont show deleted schedule
    Task ViewSchedules(PaginationDto paginationDto);
    
    // Cannot be done on active schedule or deleted schedule
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
        var schedule = new WorkSchedule
        {
            Name = generateScheduleDto.Name,
            StartDate = generateScheduleDto.StartDate.Date,
            EndDate = generateScheduleDto.EndDate.Date,
            IsActive = false,
            IsValid = true,
            ShiftAssignments = new HashSet<ShiftAssignment>(),
            Shifts = new HashSet<WorkScheduleShifts>()
        };
        
        await CreateShiftAssignmentsAsync(schedule, generateScheduleDto.ShiftIds);
    }

    private async Task CreateShiftAssignmentsAsync(WorkSchedule schedule, List<int> shiftIds)
    {
        var endDateOfSchedule = schedule.EndDate;
        var startDateOfSchedule = schedule.StartDate;
        
        var shifts = await _dbContext.Shifts
            .Include(s => s.Timeslots)
            .Include(s => s.JobRequirements)
            .Where(s => shiftIds.Contains(s.Id))
            .ToListAsync();

        if (shifts.Count != shiftIds.Count)
        {
            throw new Exception("One or more shifts were not found.");
        }

        var allTimeslots = shifts
            .SelectMany(s => s.Timeslots)
            .OrderBy(t => t.DayOfWeek)
            .ThenBy(t => t.StartTime)
            .ToList();

        if (HasIntersections(allTimeslots))
        {
            throw new Exception("Shifts have intersections with each other.");
        }
        
        var workPolicy = await _dbContext.WorkPolicies.FirstOrDefaultAsync();
        if (workPolicy == null)
        {
            throw new Exception("WorkPolicy not configured.");
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
        
        foreach (var shift in shifts)
        {
            schedule.Shifts.Add(new WorkScheduleShifts
            {
                ShiftId = shift.Id,
                WorkSchedule = schedule
            });
        }

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

            var currentDate = startDateOfSchedule.Date
                .AddDays(((int)timeslot.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7);

            var slotStart = currentDate.Add(timeslot.StartTime.ToTimeSpan());
            var slotEnd = currentDate.Add(timeslot.EndTime.ToTimeSpan());

            foreach (var req in shift.JobRequirements)
            {

                var candidates = usersByRole
                    .Where(u => u.JobRoleId == req.JobRoleId)
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
                    if (assignedCount >= req.RequiredStaffCount)
                        break;

                    // Approved absences still block assignment
                    var candidateApprovedAbsenceOverlap = approvedAbsences.Any(a =>
                        a.UserId == candidate.UserId &&
                        a.StartDate < slotEnd &&
                        slotStart < a.EndDate);

                    if (candidateApprovedAbsenceOverlap)
                        continue;

                    if (!assignmentsByUser.TryGetValue(candidate.UserId, out var userAssignments))
                    {
                        userAssignments = new List<(DateTime Start, DateTime End)>();
                        assignmentsByUser[candidate.UserId] = userAssignments;
                    }

                    var overlapsExisting = userAssignments.Any(x =>
                        x.Start < slotEnd && slotStart < x.End);

                    if (overlapsExisting)
                        continue;

                    if (!RespectsMaximumConsecutiveHours(userAssignments, slotStart, slotEnd,
                            workPolicy.MaximumConsecutiveWorkHours))
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

                if (assignedCount < req.RequiredStaffCount)
                {
                    throw new Exception(
                        $"Not enough staff for ShiftId={shift.Id}, TimeslotId={timeslot.Id}, JobRoleId={req.JobRoleId}.");
                }
            }
        }
        
        _dbContext.WorkSchedules.Add(schedule);
        await _dbContext.SaveChangesAsync();
    }

    private bool RespectsMaximumConsecutiveHours(
        List<(DateTime Start, DateTime End)> userAssignments,
        DateTime newStart,
        DateTime newEnd,
        int maximumConsecutiveWorkHours)
    {
        var chain = new List<(DateTime Start, DateTime End)>(userAssignments)
        {
            (newStart, newEnd)
        };

        chain = chain.OrderBy(x => x.Start).ToList();

        var maxConsecutive = TimeSpan.Zero;
        var currentChainStart = chain[0].Start;
        var currentChainEnd = chain[0].End;

        for (var i = 1; i < chain.Count; i++)
        {
            var next = chain[i];

            if (next.Start <= currentChainEnd)
            {
                if (next.End > currentChainEnd)
                    currentChainEnd = next.End;
            }
            else
            {
                var segment = currentChainEnd - currentChainStart;
                if (segment > maxConsecutive) maxConsecutive = segment;

                currentChainStart = next.Start;
                currentChainEnd = next.End;
            }
        }

        var finalSegment = currentChainEnd - currentChainStart;
        if (finalSegment > maxConsecutive) maxConsecutive = finalSegment;

        return maxConsecutive <= TimeSpan.FromHours(maximumConsecutiveWorkHours);
    }
    
    private bool HasIntersections(List<Timeslot> slots)
    {
        if (slots == null || slots.Count < 2) return false;

        var grouped = slots.GroupBy(s => s.DayOfWeek);

        foreach (var daySlots in grouped)
        {
            var ordered = daySlots
                .OrderBy(s => s.StartTime)
                .ToList();

            for (int i = 0; i < ordered.Count - 1; i++)
            {
                if (ordered[i + 1].StartTime < ordered[i].EndTime)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public async Task ReGenerateSchedule(int scheduleId)
    {
        var schedule = this._dbContext.WorkSchedules
            .Include(x => x.Shifts)
            .ThenInclude(x => x.Shift)
            .FirstOrDefault(x => x.Id == scheduleId);

        if (schedule == null)
        {
            throw new Exception($"Schedule with id {scheduleId} not found.");
        }
        
        await CreateShiftAssignmentsAsync(schedule, schedule.Shifts.Select(x=> x.ShiftId).ToList());
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
        var schedule = this._dbContext.WorkSchedules
            .Include(x => x.Shifts)
            .Include(x => x.ShiftAssignments)
            .FirstOrDefault(x => x.Id == scheduleId);

        if (schedule == null)
        {
            throw new Exception($"Schedule with id {scheduleId} not found.");
        }

        if (schedule.IsActive)
        {
            throw new Exception("Cannot delete active schedule");
        }
        
        this._dbContext.WorkSchedules.Remove(schedule);
        await this._dbContext.SaveChangesAsync();
    }

    public async Task SetScheduleActive(int scheduleId)
    {
        var schedule = this._dbContext.WorkSchedules
            .Include(x => x.Shifts)
            .Include(x => x.ShiftAssignments)
            .FirstOrDefault(x => x.Id == scheduleId);

        if (schedule == null)
        {
            throw new Exception($"Schedule with id {scheduleId} not found.");
        }

        if (!schedule.IsValid)
        {
            throw new Exception($"Schedule with id {scheduleId} is invalid.");
        }
        
        schedule.IsActive = true;
        await this._dbContext.SaveChangesAsync();
    }

    public async Task SetScheduleOffline(int scheduleId)
    {
        var schedule = this._dbContext.WorkSchedules
            .Include(x => x.Shifts)
            .Include(x => x.ShiftAssignments)
            .FirstOrDefault(x => x.Id == scheduleId);

        if (schedule == null)
        {
            throw new Exception($"Schedule with id {scheduleId} not found.");
        }
        
        schedule.IsActive = false;
        await this._dbContext.SaveChangesAsync();
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