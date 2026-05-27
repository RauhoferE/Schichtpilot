using AutoMapper;
using Data;
using Data.Entities;
using Microsoft.EntityFrameworkCore;
using Schichtpilot.Exceptions;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Responses;

namespace Schichtpilot.Services;

/// <summary>
/// Orchestrates shift related operations including creating, managing, deleting shifts, timeslots for the shifts and job requirements.
/// </summary>
public class ShiftService : IShiftService
{
    public ShiftService(SchichtpilotDbContext dbContext, IMapper mapper, IWorkScheduleService scheduleService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _scheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));
    }

    private readonly SchichtpilotDbContext _dbContext;

    private readonly IMapper _mapper;

    private readonly IWorkScheduleService _scheduleService;

    /// <summary>
    /// Creates a new shift.
    /// </summary>
    /// <param name="shift"> The shift to be created. </param>
    /// <returns></returns>
    /// <exception cref="AlreadyExistsException"> Thrown when a shift name with the exact name already exists. </exception>
    /// <exception cref="NotFoundException"> Thrown when a required job role could not be found. </exception>
    /// <exception cref="PolicyConflictException"> Thrown when a shift does not have enough breaks. </exception>
    public async Task CreateShiftAsync(CreateShiftDto shift)
    {
        var shiftWithSameName = this._dbContext.Shifts.FirstOrDefault(x => x.Name == shift.Name);

        if (shiftWithSameName != null)
        {
            throw new AlreadyExistsException($"Shift with name {shift.Name} already exists");
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
                JobRoleId = jobRole.Id,
                RequiredStaffCount = shiftJobRequirement.RequiredStaffCount,
            });
        }

        var missingPrerequisiteJobs = await this.GetMissingPrerequisites(shift.JobRequirements);
        if (missingPrerequisiteJobs.Any())
        {
            throw new NotFoundException(this.GenerateMissingPrerequisiteErrorMessage(missingPrerequisiteJobs));
        }

        if (!(await this.HasRequiredBreak(shift.TimeSlots)))
        {
            throw new PolicyConflictException($"Not enough breaks added in the shifts");
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

    /// <summary>
    /// Checks if each timeslot has the required amound of breaks defined in the company policy.
    /// </summary>
    /// <param name="shiftTimeSlots"> The timeslots of a shift. </param>
    /// <returns> Returns true if there are enough breaks. </returns>
    /// <exception cref="NotFoundException"> Thrown when the company policy is not yet defined. </exception>
    private async Task<bool> HasRequiredBreak(List<TimeSlotDto> shiftTimeSlots)
    {
        var companyPolicy = this._dbContext.WorkPolicies.FirstOrDefault();

        if (companyPolicy == null)
        {
            throw new NotFoundException($"Company policy needs to be defined first");
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

    /// <summary>
    /// Checks if there are enough breaks and if the working hours dont exceed the break time threshold.
    /// </summary>
    /// <param name="start"> The start time of the time slot. </param>
    /// <param name="end"> The end time of the timeslot. </param>
    /// <param name="combinedBreaks"> All breaks combined of every timeslot. </param>
    /// <param name="policy"> The current work policy. </param>
    /// <returns> Returns true if the breaks are valid. </returns>
    private bool ValidateBlock(TimeOnly start, TimeOnly end, List<BreakDto> combinedBreaks, WorkPolicy policy)
    {
        double workDurationHours = (end - start).TotalMinutes;

        // If the total work session is less than 4 hours, no break is strictly required by this rule
        if (workDurationHours <= policy.RestPeriodThresholdInMinutes) return true;

        // Check if ANY break in this block is >= 30 minutes
        // And ensure that break starts BEFORE the 4-hour mark
        bool hasValidBreak = combinedBreaks.Any(b =>
            (b.EndTime - b.StartTime).TotalMinutes >= policy.MinimumRestPeriodInMinutes &&
            (b.StartTime - start).TotalMinutes <= policy.RestPeriodThresholdInMinutes);

        return hasValidBreak;
    }

    /// <summary>
    /// Updates an existing shift with a new name and color.
    /// </summary>
    /// <param name="shiftId"> The shift to be updated. </param>
    /// <param name="shift"> The updated shift data. </param>
    /// <returns></returns>
    /// <exception cref="AlreadyExistsException"> Thrown when a shift with the same name already exists. </exception>
    /// <exception cref="NotFoundException"> Thrown when the shift could not be found. </exception>
    public async Task ManageShiftAsync(int shiftId, EditShiftDto shift)
    {
        var shiftWithSameName = this._dbContext.Shifts.FirstOrDefault(x => x.Name == shift.Name && x.Id != shiftId);

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

    /// <summary>
    /// Deletes a timeslot of a shift.
    /// </summary>
    /// <param name="shiftId"> The shift that contains the timeslot. </param>
    /// <param name="timeSlotId"> The timeslot to be deleted. </param>
    /// <returns></returns>
    /// <exception cref="NotFoundException"> Thrown when the shift or the timeslot could not be found. </exception>
    /// <exception cref="PolicyConflictException"> Thrown when there are not enough breaks defined after deleting a timeslot. </exception>
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
            throw new PolicyConflictException($"Not enough breaks added in the shifts");
        }

        shiftToDelete.Timeslots.Remove(timeSlot);
        await this._dbContext.SaveChangesAsync();

        await SetSchedulesWithShiftAsInactive(shiftId);
    }

    /// <summary>
    /// Sets all schedules that use a shift as invalid.
    /// </summary>
    /// <param name="shiftId"> The shift id. </param>
    private async Task SetSchedulesWithShiftAsInactive(int shiftId)
    {
        var schedulesWithShift = this._dbContext.WorkScheduleShifts
            .Include(x => x.WorkSchedule)
            .Include(x => x.Shift)
            .Where(x => x.ShiftId == shiftId)
            .ToList()
            .Select(x => x.WorkSchedule);

        foreach (var schedule in schedulesWithShift)
        {
            await this._scheduleService.SetScheduleOfflineAsync(schedule.Id);
            await this._scheduleService.SetScheduleAsInvalidAsync(schedule.Id);
        }
    }

    /// <summary>
    /// Adds a new timeslot to the shift.
    /// </summary>
    /// <param name="shiftId"> The shift to be updated. </param>
    /// <param name="timeSlot"> The new timeslot to be added. </param>
    /// <returns></returns>
    /// <exception cref="NotFoundException"> Thrown when the shift could not be found. </exception>
    /// <exception cref="AlreadyExistsException"> Thrown when there is already a timeslot for this day. </exception>
    /// <exception cref="PolicyConflictException"> Thrown when there are not enough breaks defined. </exception>
    public async Task AddTimeSlotAsync(int shiftId, TimeSlotDto timeSlot)
    {
        var shiftToModiy = this._dbContext.Shifts
            .Include(x => x.Timeslots)
            .ThenInclude(x => x.Breaks)
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
            throw new PolicyConflictException($"Not enough breaks added in the shifts");
        }

        await this._dbContext.SaveChangesAsync();

        await SetSchedulesWithShiftAsInactive(shiftId);
    }

    /// <summary>
    /// Updates an existing timeslot.
    /// </summary>
    /// <param name="shiftId"> The shift that contains the timeslot. </param>
    /// <param name="timeSlot"> The timeslot to be updated. </param>
    /// <returns></returns>
    /// <exception cref="NotFoundException"> Thrown when the shift could not be found. </exception>
    /// <exception cref="AlreadyExistsException"> Thrown when there is already a timeslot for this day. </exception>
    /// <exception cref="PolicyConflictException"> Thrown when there are not enough breaks defined. </exception>
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
            throw new PolicyConflictException($"Not enough breaks added in the shifts");
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

    /// <summary>
    /// Adds new job requirements for a shift.
    /// </summary>
    /// <param name="shiftId"> The shift to be updated. </param>
    /// <param name="jobRequirement"> The new job requirements. </param>
    /// <returns></returns>
    /// <exception cref="NotFoundException"> Thrown when the shift or the job role could not be found. </exception>
    /// <exception cref="AlreadyExistsException"> Thrown if the job requirement was already added. </exception>
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
        if (shiftToModiy.JobRequirements.FirstOrDefault(x => x.JobRoleId == jobRequirement.JobId) != null)
        {
            throw new AlreadyExistsException($"Job already added to shift!");
        }

        var jobRole = this._dbContext.JobRoles.FirstOrDefault(x => x.Id == jobRequirement.JobId);

        if (jobRole == null)
        {
            throw new NotFoundException($"Job role with id {jobRequirement.JobId} does not exist");
        }

        var jobsToCheck = shiftToModiy.JobRequirements.Select(x => this._mapper.Map<ShiftRequirement, ShiftRequirementDto>(x)).ToList();
        jobsToCheck.Add(jobRequirement);
        var missingPrerequisiteJobs = await this.GetMissingPrerequisites(jobsToCheck);
        
        if (missingPrerequisiteJobs.Any())
        {
            throw new NotFoundException(this.GenerateMissingPrerequisiteErrorMessage(missingPrerequisiteJobs));
        }

        shiftToModiy.JobRequirements.Add(new ShiftRequirement()
        {
            JobRoleId = jobRequirement.JobId,
            RequiredStaffCount = jobRequirement.RequiredStaffCount,

        });

        await this._dbContext.SaveChangesAsync();

        await SetSchedulesWithShiftAsInactive(shiftId);
    }

    private string GenerateMissingPrerequisiteErrorMessage(List<(string, string)> missingPrerequisiteJobs)
    {
        var errorText = "";
        foreach (var missingPrerequisiteJob in missingPrerequisiteJobs)
        {
            errorText = errorText + 
                        $"{missingPrerequisiteJob.Item2} requires {missingPrerequisiteJob.Item1}\n";
        }
        
        return errorText;
    }

    /// <summary>
    /// Changes the staff amount needed to complete a shift.
    /// </summary>
    /// <param name="shiftId"> The shift to be updated. </param>
    /// <param name="jobRequirementId"> The job to be updated. </param>
    /// <param name="requiredStaffCount"> The staff needed for this particular job. </param>
    /// <returns></returns>
    /// <exception cref="NotFoundException"> Thrown when the job role or the shift could not be found. </exception>
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

        var jobRequirement = shiftToModiy.JobRequirements.FirstOrDefault(x => x.JobRoleId == jobRequirementId);

        if (jobRequirement == null)
        {
            throw new NotFoundException($"Job requirement with id {jobRequirementId} does not exist");
        }

        jobRequirement.RequiredStaffCount = requiredStaffCount;
        await this._dbContext.SaveChangesAsync();

        await SetSchedulesWithShiftAsInactive(shiftId);
    }

    /// <summary>
    /// Deletes a job requirement of a shift.
    /// </summary>
    /// <param name="shiftId"> The shift to be updated. </param>
    /// <param name="jobRequirementId"> The job to be removed from a shift. </param>
    /// <returns></returns>
    /// <exception cref="NotFoundException"> Thrown when the job role or the shift could not be found. </exception>
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

        var jobRequirement = shiftToModiy.JobRequirements.FirstOrDefault(x => x.JobRoleId == jobRequirementId);

        if (jobRequirement == null)
        {
            throw new NotFoundException($"Job requirement with id {jobRequirementId} does not exist");
        }
        
        var jobsToCheck = shiftToModiy.JobRequirements.Select(x => this._mapper.Map<ShiftRequirement, ShiftRequirementDto>(x)).ToList();
        jobsToCheck = jobsToCheck.Where(x => x.JobId != jobRequirementId).ToList();
        var missingPrerequisiteJobs = await this.GetMissingPrerequisites(jobsToCheck);
        
        if (missingPrerequisiteJobs.Any())
        {
            throw new NotFoundException(this.GenerateMissingPrerequisiteErrorMessage(missingPrerequisiteJobs));
        }

        await SetSchedulesWithShiftAsInactive(shiftId);

        shiftToModiy.JobRequirements.Remove(jobRequirement);
        await this._dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Returns all job roles, even their dependencies required for the shift. 
    /// </summary>
    /// <param name="requirements"> The job roles required for the shift. </param>
    /// <returns>Returns all job roles, even their dependencies required for the shift. </returns>
    private async Task<List<(string, string)>> GetMissingPrerequisites(List<ShiftRequirementDto> requirements)
    {
        var requestedJobIds = requirements.Select(r => r.JobId).ToHashSet();
        // Dependency and which role depends on it
        var allRequiredIds = new HashSet<Tuple<int, int>>();

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
                .Where(l => l.JobRoleId == currentJobId)
                .Select(l => l.DependencyJobRoleId);

            foreach (var prereqId in prerequisites)
            {
                // Only add to queue if we haven't processed this requirement yet
                if (allRequiredIds.Add(new Tuple<int, int>(prereqId, currentJobId)))
                {
                    processingQueue.Enqueue(prereqId);
                }
            }
        }

        // 4. Identify IDs that are required but missing from the original request
        //var missingIds = allRequiredIds.Except(requestedJobIds).ToList();
        var missingIds = allRequiredIds.Where(x => !requestedJobIds.Contains(x.Item1)).ToList();

        if (!missingIds.Any())
        {
            return [];
        }
        
        var missingRole = new List<(string, string)>();
        foreach (var missingIdsTuple in missingIds)
        {
            var dependency = this._dbContext.JobRoles.FirstOrDefault(x => x.Id == missingIdsTuple.Item1);
            var dependents = this._dbContext.JobRoles.FirstOrDefault(x => x.Id == missingIdsTuple.Item2);

            if (dependency != null && dependents != null)
            {
                missingRole.Add((dependency.Name, dependents.Name));
            }
            
        }

        return missingRole;

        // 5. Fetch names for the missing roles
        //return await this._dbContext.JobRoles
        //    .Where(r => missingIds.Contains(r.Id))
        //    .Select(r => r.Name)
        //    .ToListAsync();
    }

    /// <summary>
    /// Deletes an existing shift.
    /// </summary>
    /// <param name="shiftId"> The shift to be deleted. </param>
    /// <returns></returns>
    /// <exception cref="NotFoundException"> Thrown when the shift could not be found. </exception>
    public async Task DeleteShiftAsync(int shiftId)
    {
        var shiftToDelete = this._dbContext.Shifts
            .Include(x => x.Timeslots)
            .ThenInclude(x => x.Breaks)
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

    /// <summary>
    /// Gets a list of existing shifts.
    /// </summary>
    /// <param name="pagination"> The pagination element. </param>
    /// <param name="filter"> How to sort and filter the shifts. </param>
    /// <returns> Returns the shifts as <see cref="QueryableShiftResponse"/>. </returns>
    public async Task<QueryableShiftResponse> GetShiftsAsync(PaginationDto pagination, ShiftFilterDto? filter)
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

    /// <summary>
    /// Filters the given list of shifts.
    /// </summary>
    /// <param name="query"> The list of shifts. </param>
    /// <param name="filter"> How the shifts should be filtered. </param>
    /// <returns> Returns the shifts as <see cref="IQueryable"/>. </returns>
    private async Task<IQueryable<Shift>> FilterShiftsAsync(IQueryable<Shift> query, ShiftFilterDto filter)
    {
        if (!string.IsNullOrEmpty(filter.Searchstring))
        {
            query = query.Where(x => x.Name.ToLower().Contains(filter.Searchstring.ToLower()));
        }

        if (filter.WeekDays.Count > 0)
        {
            query = query.Where(x => x.Timeslots.Any(y => filter.WeekDays.Contains(y.DayOfWeek)));
        }

        //TODO: Add schedule status as filter
        return query;
    }

    /// <summary>
    /// Gets the details of a shift.
    /// </summary>
    /// <param name="shiftId"> The targeted shift. </param>
    /// <returns> Returns the shift as <see cref="ShiftDto"/>. </returns>
    /// <exception cref="NotFoundException"> Thrown when the shift could not be found. </exception>
    public async Task<ShiftDto> GetShiftAsync(int shiftId)
    {
        //TODO: Return schedules that use this shift
        var shift = this._dbContext.Shifts
            .Include(x => x.JobRequirements)
            .ThenInclude(x => x.JobRole)
            .Include(x => x.Timeslots)
            .ThenInclude(x => x.Breaks)
            .FirstOrDefault(x => x.Id == shiftId);

        if (shift == null)
        {
            throw new NotFoundException($"Shift with id {shiftId} does not exist");
        }

        return this._mapper.Map<Shift, ShiftDto>(shift);
    }
}