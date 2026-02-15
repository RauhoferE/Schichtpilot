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
    public ShiftService(SchichtpilotDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    private readonly SchichtpilotDbContext _dbContext;
    
    private readonly IMapper _mapper;
    
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

        this._dbContext.Shifts.Add(new Shift()
        {
            Name = shift.Name,
            ColorAsHex = shift.ColorAsHex,
            Timeslots = shift.TimeSlots.Select(x => new Timeslot()
            {
                DayOfWeek = x.DayOfWeek,
                StartTime = x.StartTime,
                EndTime = x.EndTime
            }).ToHashSet(),
            JobRequirements = jobRoleRequirementsForShift.ToHashSet()
        });

        await this._dbContext.SaveChangesAsync();
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
        //TODO: Check if shift is used in a schedule
        var shiftToDelete = this._dbContext.Shifts
            .Include(x => x.Timeslots)
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
        
        shiftToDelete.Timeslots.Remove(timeSlot);
        await this._dbContext.SaveChangesAsync();
    }

    public async Task AddTimeSlotAsync(int shiftId, TimeSlotDto timeSlot)
    {
        //TODO: Check if shift is used in a schedule
        var shiftToModiy = this._dbContext.Shifts
            .Include(x => x.Timeslots)
            .FirstOrDefault(x => x.Id == shiftId);

        if (shiftToModiy == null)
        {
            throw new NotFoundException($"Shift with id {shiftId} does not exist");
        }
        
        // Check if there is already a timeslot here
        var timeSlotsOnSameDay = shiftToModiy.Timeslots
            .FirstOrDefault(x => x.DayOfWeek == timeSlot.DayOfWeek && timeSlot.StartTime < x.EndTime 
                                                          && x.StartTime < timeSlot.EndTime);

        if (timeSlotsOnSameDay != null)
        {
            throw new AlreadyExistsException($"Timeslot for ${timeSlot.DayOfWeek} already exists.");
        }

        shiftToModiy.Timeslots.Add(new Timeslot()
        {
            DayOfWeek = timeSlot.DayOfWeek,
            StartTime = timeSlot.StartTime,
            EndTime = timeSlot.EndTime
        });

        await this._dbContext.SaveChangesAsync();
    }

    public async Task EditTimeSlotAsync(int shiftId, TimeSlotDto timeSlot)
    {
        //TODO: Check if shift is used in a schedule
        var shiftToModiy = this._dbContext.Shifts
            .Include(x => x.Timeslots)
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
            .FirstOrDefault(x => x.DayOfWeek == timeSlot.DayOfWeek && timeSlot.StartTime < x.EndTime 
                                                          && x.StartTime < timeSlot.EndTime && x.Id != timeSlot.Id);

        if (timeSlotsOnSameDay != null)
        {
            throw new AlreadyExistsException($"Timeslot for ${timeSlot.DayOfWeek} already exists.");
        }

        timeSlotToModify.DayOfWeek = timeSlot.DayOfWeek;
        timeSlotToModify.StartTime = timeSlot.StartTime;
        timeSlotToModify.EndTime = timeSlot.EndTime;

        await this._dbContext.SaveChangesAsync();
    }

    public async Task AddJobRequirementAsync(int shiftId, ShiftRequirementDto jobRequirement)
    {
        //TODO: Check if shift is used in a schedule
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
    }

    public async Task ChangeRequiredStaffAsync(int shiftId, int jobRequirementId, int requiredStaffCount)
    {
        //TODO: Check if shift is used in a schedule
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
    }

    public async Task DeleteJobRequirementAsync(int shiftId, int jobRequirementId)
    {
        //TODO: Check if shift is used in a schedule
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
        //TODO: Check if shift is used in a schedule
        var shiftToDelete = this._dbContext.Shifts.FirstOrDefault(x => x.Id == shiftId);

        if (shiftToDelete == null)
        {
            throw new NotFoundException($"Shift with id {shiftId} does not exist");
        }
        
        this._dbContext.Shifts.Remove(shiftToDelete);
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
            .FirstOrDefault(x => x.Id == shiftId);

        if (shift == null)
        {
            throw new NotFoundException($"Shift with id {shiftId} does not exist");
        }

        return this._mapper.Map<Shift, ShiftDto>(shift);
    }
}