using AutoMapper;
using Data;
using Data.Entities;
using Microsoft.EntityFrameworkCore;
using Schichtpilot.Exceptions;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Responses;

namespace Schichtpilot.Services;

public class ShiftService : IShiftService
{
    public ShiftService(SchichtpilotDbContext dbContext, IMapper mapper, IWorkScheduleService  scheduleService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _scheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));
    }

    private readonly SchichtpilotDbContext _dbContext;
    
    private readonly IMapper _mapper;
    
    private readonly IWorkScheduleService  _scheduleService;
    
    public async Task CreateShiftAsync(CreateShiftDto shift)
    {
        var shiftWithSameName = this._dbContext.Shifts.FirstOrDefault(x =>  x.Name == shift.Name);

        if (shiftWithSameName != null)
        {
            throw new Exception($"Shift with name {shift.Name} already exists");
        }

        var jobRoleRequirementsForShift = new List<ShiftRequirement>();
        foreach (var shiftJobRequirement in shift.JobRequirements)
        {
            var jobRole = this._dbContext.JobRoles.FirstOrDefault(x => x.Id == shiftJobRequirement.JobId);
            if (jobRole == null)
            {
                throw new NotFoundException($"Job role {shiftJobRequirement.JobId} does not exist");
            }

            jobRoleRequirementsForShift.Add(new ShiftRequirement()
            {
                JobRole = jobRole,
                JobRoleId = jobRole.Id,
                RequiredStaffCount = shiftJobRequirement.RequiredStaffCount,
            });
        }
        
        var missingPrerequisiteJobs = await this.GetMissingPrerequisites(shift.JobRequirements);
        if (missingPrerequisiteJobs.Any())
        {
            throw new NotFoundException($"The following Job roles are missing: {string.Join(", ", missingPrerequisiteJobs)}");
        }

        if (!(await this.HasRequiredBreak(shift.TimeSlots)))
        {
            //TODO: Add Custom Exception
            throw new Exception($"Not enough breaks added in the shifts");
        }

        this._dbContext.Shifts.Add(new Shift()
        {
            Name = shift.Name,
            ColorAsHex = shift.ColorAsHex,
            Timeslots = shift.TimeSlots.Select(x => new Timeslot()
            {
                DayOfWeek = x.DayOfWeek,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Breaks = x.Breaks.Select(b =>
                    new Break()
                    {
                        StartTime = b.StartTime,
                        EndTime = b.EndTime
                    }
                ).ToHashSet()
            }).ToHashSet(),
            JobRequirements = jobRoleRequirementsForShift.ToHashSet()
        });

        await this._dbContext.SaveChangesAsync();
    }

    private async Task<bool> HasRequiredBreak(List<TimeSlotDto> shiftTimeSlots)
    {
        var companyPolicy = this._dbContext.WorkPolicies.FirstOrDefault();

        if (companyPolicy == null)
        {
            //TODO: Add custom exception
            throw new Exception($"Company policy needs to be defined first");
        }
        
// 1. Order slots chronologically to handle the "Midnight Stitch"
        // We use a helper to turn DayOfWeek + TimeOnly into a relative linear offset
        var sortedSlots = shiftTimeSlots
            .OrderBy(s => (int)s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToList();

        for (int i = 0; i < sortedSlots.Count; i++)
        {
            var currentSlot = sortedSlots[i];
            
            // 2. Identify Continuous Work Block (Stitching)
            var workBlockEnd = currentSlot.EndTime;
            var combinedBreaks = new List<BreakDto>(currentSlot.Breaks);
            
            // Look ahead to see if the next slot "stitches" (Midnight wrap)
            int nextIndex = (i + 1) % sortedSlots.Count;
            var nextSlot = sortedSlots[nextIndex];

            // If current ends at 23:59:59 and next starts at 00:00:00 on the following day
            if (currentSlot.EndTime.Hour == 23 && currentSlot.EndTime.Minute == 59 &&
                nextSlot.StartTime.Hour == 0 && nextSlot.StartTime.Minute == 0 &&
                (int)nextSlot.DayOfWeek == ((int)currentSlot.DayOfWeek + 1) % 7)
            {
                workBlockEnd = nextSlot.EndTime;
                combinedBreaks.AddRange(nextSlot.Breaks);
            }

            // 3. Validate the 4-hour rule
            if (!ValidateBlock(currentSlot.StartTime, workBlockEnd, combinedBreaks, companyPolicy))
                return false;
        }

        return true;
    }

    private bool ValidateBlock(TimeOnly start, TimeOnly end, List<BreakDto> combinedBreaks, WorkPolicy policy)
    {
        double workDurationHours = (end - start).TotalMinutes;
        
        // If the total work session is less than 4 hours, no break is strictly required by this rule
        if (workDurationHours <= policy.RestPeriodThresholdInMinutes) return true;

        // Check if ANY break in this block is >= 30 minutes
        // And ensure that break starts BEFORE the 4-hour mark
        bool hasValidBreak = combinedBreaks.Any(b => 
            (b.EndTime - b.StartTime).TotalMinutes >= policy.RestPeriodInMinutes && 
            (b.StartTime - start).TotalMinutes <= policy.RestPeriodThresholdInMinutes);

        return hasValidBreak;
    }

    public async Task ManageShiftAsync(int shiftId, EditShiftDto shift)
    {
        var shiftWithSameName = this._dbContext.Shifts.FirstOrDefault(x =>  x.Name == shift.Name && x.Id != shiftId);

        if (shiftWithSameName != null)
        {
            throw new AlreadyExistsException($"Shift with name {shift.Name} already exists");
        }

        var shiftToModify = this._dbContext.Shifts.FirstOrDefault(x => x.Id == shiftId);

        if (shiftToModify == null)
        {
            throw new NotFoundException($"Shift with id {shiftId} does not exist");
        }
        
        shiftToModify.Name = shift.Name;
        shiftToModify.ColorAsHex = shift.ColorAsHex;
        await this._dbContext.SaveChangesAsync();
    }

    public async Task DeleteTimeSlotAsync(int shiftId, int timeSlotId)
    {
        var shiftToDelete = this._dbContext.Shifts
            .Include(x => x.Timeslots)
            .ThenInclude(x => x.Breaks)
            .FirstOrDefault(x => x.Id == shiftId);

        if (shiftToDelete == null)
        {
            throw new NotFoundException($"Shift with id {shiftId} does not exist");
        }

        var timeSlot = shiftToDelete.Timeslots.FirstOrDefault(x => x.Id == timeSlotId);

        if (timeSlot == null)
        {
            throw new NotFoundException($"Timeslot with {timeSlotId} does not exist!");
        }
        
        if (!(await this.HasRequiredBreak(shiftToDelete.Timeslots.Where(x => x.Id != timeSlotId).Select(x => this._mapper.Map<Timeslot, TimeSlotDto>(x)).ToList())))
        {
            //TODO: Add Custom Exception
            throw new Exception($"Not enough breaks added in the shifts");
        }
        
        shiftToDelete.Timeslots.Remove(timeSlot);
        await this._dbContext.SaveChangesAsync();
        
        await SetSchedulesWithShiftAsInactive(shiftId);
    }

    private async Task SetSchedulesWithShiftAsInactive(int shiftId)
    {
        var schedulesWithShift = this._dbContext.WorkScheduleShifts
            .Include(x => x.WorkSchedule)
            .Include(x => x.Shift)
            .Where(x=> x.ShiftId ==  shiftId)
            .ToList()
            .Select(x => x.WorkSchedule);

        foreach (var schedule in schedulesWithShift)
        {
            await this._scheduleService.SetScheduleOfflineAsync(schedule.Id);
            await this._scheduleService.SetScheduleAsInvalidAsync(schedule.Id);
        }
    }

    public async Task AddTimeSlotAsync(int shiftId, TimeSlotDto timeSlot)
    {
        var shiftToModiy = this._dbContext.Shifts
            .Include(x => x.Timeslots)
            .FirstOrDefault(x => x.Id == shiftId);

        if (shiftToModiy == null)
        {
            throw new NotFoundException($"Shift with id {shiftId} does not exist");
        }
        
        // Check if there is already a timeslot here
        // Only one timeslot per day
        var timeSlotsOnSameDay = shiftToModiy.Timeslots
            .FirstOrDefault(x => x.DayOfWeek == timeSlot.DayOfWeek);

        if (timeSlotsOnSameDay != null)
        {
            throw new AlreadyExistsException($"Timeslot for ${timeSlot.DayOfWeek} already exists.");
        }
        
        shiftToModiy.Timeslots.Add(new Timeslot()
        {
            DayOfWeek = timeSlot.DayOfWeek,
            StartTime = timeSlot.StartTime,
            EndTime = timeSlot.EndTime,
            Breaks = timeSlot.Breaks.Select(x => new Break()
            {
                StartTime = x.StartTime,
                EndTime = x.EndTime
            }).ToHashSet()
        });
        
        if (!(await this.HasRequiredBreak(shiftToModiy.Timeslots.Select(x => this._mapper.Map<Timeslot, TimeSlotDto>(x)).ToList())))
        {
            //TODO: Add Custom Exception
            throw new Exception($"Not enough breaks added in the shifts");
        }

        await this._dbContext.SaveChangesAsync();
        
        await SetSchedulesWithShiftAsInactive(shiftId);
    }

    public async Task EditTimeSlotAsync(int shiftId, TimeSlotDto timeSlot)
    {
        var shiftToModiy = this._dbContext.Shifts
            .Include(x => x.Timeslots)
            .ThenInclude(x => x.Breaks)
            .FirstOrDefault(x => x.Id == shiftId);

        if (shiftToModiy == null)
        {
            throw new NotFoundException($"Shift with id {shiftId} does not exist");
        }
        
        var timeSlotToModify = shiftToModiy.Timeslots.FirstOrDefault(x => x.Id == timeSlot.Id);

        if (timeSlotToModify == null)
        {
            throw new NotFoundException($"Timeslot with ${timeSlot.Id} not found.");
        }
        
        // Check if there is already a timeslot here thats not the current one
        var timeSlotsOnSameDay = shiftToModiy.Timeslots
            .FirstOrDefault(x => x.DayOfWeek == timeSlot.DayOfWeek && x.Id != timeSlot.Id);

        if (timeSlotsOnSameDay != null)
        {
            throw new AlreadyExistsException($"Timeslot for ${timeSlot.DayOfWeek} already exists.");
        }

        var timeSlotsToCheck = shiftToModiy.Timeslots.Select(x => this._mapper.Map<Timeslot, TimeSlotDto>(x)).ToList();
        var indexOfTimeSlotToModify = timeSlotsToCheck.FindIndex(x => x.Id == timeSlot.Id);
        timeSlotsToCheck[indexOfTimeSlotToModify].DayOfWeek = timeSlot.DayOfWeek;
        timeSlotsToCheck[indexOfTimeSlotToModify].StartTime = timeSlot.StartTime;
        timeSlotsToCheck[indexOfTimeSlotToModify].EndTime = timeSlot.EndTime;
        timeSlotsToCheck[indexOfTimeSlotToModify].Breaks = timeSlot.Breaks;
        
        if (!(await this.HasRequiredBreak(timeSlotsToCheck)))
        {
            //TODO: Add Custom Exception
            throw new Exception($"Not enough breaks added in the shifts");
        }
        
        this._dbContext.Breaks.RemoveRange(timeSlotToModify.Breaks);
        await this._dbContext.SaveChangesAsync();

        timeSlotToModify.DayOfWeek = timeSlot.DayOfWeek;
        timeSlotToModify.StartTime = timeSlot.StartTime;
        timeSlotToModify.EndTime = timeSlot.EndTime;
        timeSlotToModify.Breaks = timeSlot.Breaks.Select(x => new Break()
        {
            StartTime = x.StartTime,
            EndTime = x.EndTime
        }).ToHashSet();

        await this._dbContext.SaveChangesAsync();
        
        await SetSchedulesWithShiftAsInactive(shiftId);
    }

    public async Task AddJobRequirementAsync(int shiftId, ShiftRequirementDto jobRequirement)
    {
        var shiftToModiy = this._dbContext.Shifts
            .Include(x => x.JobRequirements)
            .ThenInclude(x => x.JobRole)
            .FirstOrDefault(x => x.Id == shiftId);

        if (shiftToModiy == null)
        {
            throw new NotFoundException($"Shift with id {shiftId} does not exist");
        }

        // Job already added as requirement
        if (shiftToModiy.JobRequirements.FirstOrDefault(x => x.JobRoleId == jobRequirement.JobId)  != null)
        {
            throw new AlreadyExistsException($"Job already added to shift!");
        }
        
        var jobRole = this._dbContext.JobRoles.FirstOrDefault(x => x.Id == jobRequirement.JobId);

        if (jobRole == null)
        {
            throw new NotFoundException($"Job role with id {jobRequirement.JobId} does not exist");
        }

        shiftToModiy.JobRequirements.Add(new ShiftRequirement()
        {
            JobRole = jobRole,
            JobRoleId = jobRequirement.JobId,
            RequiredStaffCount = jobRequirement.RequiredStaffCount,

        });
        
        await this._dbContext.SaveChangesAsync();
        
        await SetSchedulesWithShiftAsInactive(shiftId);
    }

    public async Task ChangeRequiredStaffAsync(int shiftId, int jobRequirementId, int requiredStaffCount)
    {
        var shiftToModiy = this._dbContext.Shifts
            .Include(x => x.JobRequirements)
            .ThenInclude(x => x.JobRole)
            .FirstOrDefault(x => x.Id == shiftId);

        if (shiftToModiy == null)
        {
            throw new NotFoundException($"Shift with id {shiftId} does not exist");
        }
        
        var jobRequirement = shiftToModiy.JobRequirements.FirstOrDefault(x => x.Id == jobRequirementId);

        if (jobRequirement == null)
        {
            throw new NotFoundException($"Job requirement with id {jobRequirementId} does not exist");
        }
        
        jobRequirement.RequiredStaffCount = requiredStaffCount;
        await this._dbContext.SaveChangesAsync();
        
        await SetSchedulesWithShiftAsInactive(shiftId);
    }

    public async Task DeleteJobRequirementAsync(int shiftId, int jobRequirementId)
    {
        var shiftToModiy = this._dbContext.Shifts
            .Include(x => x.JobRequirements)
            .ThenInclude(x => x.JobRole)
            .FirstOrDefault(x => x.Id == shiftId);

        if (shiftToModiy == null)
        {
            throw new NotFoundException($"Shift with id {shiftId} does not exist");
        }
        
        var jobRequirement = shiftToModiy.JobRequirements.FirstOrDefault(x => x.Id == jobRequirementId);

        if (jobRequirement == null)
        {
            throw new NotFoundException($"Job requirement with id {jobRequirementId} does not exist");
        }
        
        await SetSchedulesWithShiftAsInactive(shiftId);
        
        shiftToModiy.JobRequirements.Remove(jobRequirement);
        await this._dbContext.SaveChangesAsync();
    }

    private async Task<List<string>> GetMissingPrerequisites(List<ShiftRequirementDto> requirements)
    {
        var requestedJobIds = requirements.Select(r => r.JobId).ToHashSet();
        var allRequiredIds = new HashSet<int>();
    
        // 1. Get all dependency links from the DB
        var allLinks = await this._dbContext.JobRoleDependencies.ToListAsync();

        // 2. Initialize a Queue with the jobs currently in the shift
        var processingQueue = new Queue<int>(requestedJobIds);

        // 3. Process the queue until no more prerequisites are found
        while (processingQueue.Count > 0)
        {
            var currentJobId = processingQueue.Dequeue();

            // Find what the current job depends on
            var prerequisites = allLinks
                .Where(l => l.DependencyJobRoleId == currentJobId)
                .Select(l => l.JobRoleId);

            foreach (var prereqId in prerequisites)
            {
                // Only add to queue if we haven't processed this requirement yet
                if (allRequiredIds.Add(prereqId))
                {
                    processingQueue.Enqueue(prereqId);
                }
            }
        }

        // 4. Identify IDs that are required but missing from the original request
        var missingIds = allRequiredIds.Except(requestedJobIds).ToList();

        if (!missingIds.Any())
        {
            return [];
        }

        // 5. Fetch names for the missing roles
        return await this._dbContext.JobRoles
            .Where(r => missingIds.Contains(r.Id))
            .Select(r => r.Name)
            .ToListAsync();
    }

    public async Task DeleteShiftAsync(int shiftId)
    {
        var shiftToDelete = this._dbContext.Shifts
            .Include(x => x.Timeslots)
            .ThenInclude(x=> x.Breaks)
            .Include(x => x.JobRequirements)
            .FirstOrDefault(x => x.Id == shiftId);

        if (shiftToDelete == null)
        {
            throw new NotFoundException($"Shift with id {shiftId} does not exist");
        }
        
        this._dbContext.Shifts.Remove(shiftToDelete);
        await SetSchedulesWithShiftAsInactive(shiftId);
        await this._dbContext.SaveChangesAsync();
    }

    public async Task<QueryableShiftResponse> ViewShiftsAsync(PaginationDto pagination, ShiftFilterDto? filter)
    {
        //TODO: Add if shift is used in a schedule.
        IQueryable<Shift> query = this._dbContext.Shifts
            .Include(x => x.JobRequirements)
            .Include(x => x.Timeslots)
            .AsQueryable();

        if (filter != null)
        {
            query = await this.FilterShiftsAsync(query, filter);
        }

        return new QueryableShiftResponse()
        {
            Count = query.Count(),
            Shift = query
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(x => this._mapper.Map<Shift, ShortShiftDto>(x))
        };
    }

    private async Task<IQueryable<Shift>> FilterShiftsAsync(IQueryable<Shift> query, ShiftFilterDto filter)
    {
        if (!string.IsNullOrEmpty(filter.Searchstring))
        {
            query = query.Where(x => x.Name.ToLower().Contains(filter.Searchstring.ToLower()));
        }

        if (filter.WeekDays.Count > 0)
        {
            query = query.Where(x => x.Timeslots.Any(y =>  filter.WeekDays.Contains(y.DayOfWeek)));
        }
        
        //TODO: Add schedule status as filter
        return query;
    }

    public async Task<ShiftDto> GetShiftAsync(int shiftId)
    {
        //TODO: Return schedules that use this shift
        var shift = this._dbContext.Shifts
            .Include(x => x.JobRequirements)
            .ThenInclude(x => x.JobRole)
            .Include(x => x.Timeslots)
            .ThenInclude(x => x.Breaks )
            .FirstOrDefault(x => x.Id == shiftId);

        if (shift == null)
        {
            throw new NotFoundException($"Shift with id {shiftId} does not exist");
        }

        return this._mapper.Map<Shift, ShiftDto>(shift);
    }
}