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

    public Task ManageShiftAsync(int shiftId, CreateShiftDto shift)
    {
        throw new NotImplementedException();
    }

    public Task DeleteShiftAsync(int shiftId)
    {
        throw new NotImplementedException();
    }

    public Task<QueryableShiftResponse> ViewShiftsAsync(PaginationDto pagination, ShiftFilterDto? filter)
    {
        throw new NotImplementedException();
    }

    public Task<ShiftDto> GetShiftAsync(int shiftId)
    {
        throw new NotImplementedException();
    }
}