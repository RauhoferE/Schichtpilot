using AutoMapper;
using Data;
using Data.Entities;
using Microsoft.EntityFrameworkCore;
using Schichtpilot.Exceptions;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Services;

public class JobRoleService : IJobRoleService
{
    public JobRoleService(SchichtpilotDbContext dbContext, IMapper mapper, IWorkScheduleService  workScheduleService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _workScheduleService = workScheduleService ?? throw new ArgumentNullException(nameof(workScheduleService));
    }

    private readonly SchichtpilotDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IWorkScheduleService  _workScheduleService;


    public async Task CreateJobRoleAsync(CreateJobRoleDto jobRole)
    {
        if (this._dbContext.JobRoles.Any(jr => jr.Name == jobRole.Name))
        {
            throw new AlreadyExistsException($"Jobrole  with name {jobRole.Name} already exists");
        }

        this._dbContext.JobRoles.Add(new JobRole()
        {
            Name = jobRole.Name,
            Description = jobRole.Description,
        });

        await this._dbContext.SaveChangesAsync();

        var createdJobRole = await this._dbContext.JobRoles.FirstOrDefaultAsync(jr => jr.Name == jobRole.Name);

        foreach (var jobRoleIds in jobRole.DependentOnJobRoleIds)
        {
            var dependentJobRole = this._dbContext.JobRoles
                .FirstOrDefault(jr => jr.Id == jobRoleIds);
            if (dependentJobRole != null)
            {
                this._dbContext.JobRoleDependencies.Add(new JobRoleDependency()
                {
                    DependencyJobRoleId = dependentJobRole.Id,
                    Dependency = dependentJobRole,
                    JobRole = createdJobRole,
                    JobRoleId = createdJobRole.Id
                });
            }
        }

        await this._dbContext.SaveChangesAsync();
    }

    // This just updates the name and description
    public async Task UpdateJobRoleAsync(int id, EditJobRoleDto jobRole)
    {
        if (this._dbContext.JobRoles.Any(jr => jr.Name == jobRole.Name && jr.Id != id))
        {
            throw new AlreadyExistsException($"Jobrole  with name {jobRole.Name} already exists");
        }

        var jobRoleToModify = await this._dbContext.JobRoles.FirstOrDefaultAsync(jr => jr.Id == id);

        if (jobRoleToModify == null)
        {
            throw new NotFoundException("Jobrole not found!");
        }

        jobRoleToModify.Name = jobRole.Name;
        jobRoleToModify.Description = jobRole.Description;

        await this._dbContext.SaveChangesAsync();
    }

    public async Task AddDependenciesToJobRoleAsync(int jobRoleId, int dependencyId)
    {
        //TODO: Check if jobrole is used in a schedule
        var jobRole = await this._dbContext.JobRoles.FirstOrDefaultAsync(jr => jr.Id == jobRoleId);
        var dependencyJobRole = await this._dbContext.JobRoles.FirstOrDefaultAsync(jr => jr.Id == dependencyId);

        if (jobRole == null)
        {
            throw new NotFoundException("Jobrole not found!");
        }

        if (dependencyJobRole == null)
        {
            throw new NotFoundException("Dependency not found!");
        }

        var dependency =
            this._dbContext.JobRoleDependencies.FirstOrDefault(x =>
                x.JobRoleId == jobRoleId && x.DependencyJobRoleId == dependencyId);

        if (dependency != null)
        {
            throw new AlreadyExistsException("Dependency already exisits!");
        }

        if (await this.WouldCreateCycle(dependencyId, jobRoleId))
        {
            throw new InvalidOperationException("Circular dependency detected!");
        }

        this._dbContext.JobRoleDependencies.Add(new JobRoleDependency()
        {
            DependencyJobRoleId = dependencyJobRole.Id,
            Dependency = dependencyJobRole,
            JobRole = jobRole,
            JobRoleId = jobRole.Id
        });

        await this._dbContext.SaveChangesAsync();
        
    }

    public async Task RemoveDependenciesToJobRoleAsync(int jobRoleId, int dependencyId)
    {
        var jobRole = await this._dbContext.JobRoles.FirstOrDefaultAsync(jr => jr.Id == jobRoleId);
        var dependencyJobRole = await this._dbContext.JobRoles.FirstOrDefaultAsync(jr => jr.Id == dependencyId);

        if (jobRole == null)
        {
            throw new NotFoundException("Jobrole not found!");
        }

        if (dependencyJobRole == null)
        {
            throw new NotFoundException("Dependency not found!");
        }

        var dependency =
            this._dbContext.JobRoleDependencies.FirstOrDefault(x =>
                x.JobRoleId == jobRoleId && x.DependencyJobRoleId == dependencyId);

        if (dependency != null)
        {
            this._dbContext.JobRoleDependencies.Remove(dependency);
            await this._dbContext.SaveChangesAsync();
        }
    }

    public async Task AddUsersToJobRoleAsync(int id, List<long> userIds)
    {
        var jobRoleToModify = await this._dbContext.JobRoles.FirstOrDefaultAsync(jr => jr.Id == id);

        if (jobRoleToModify == null)
        {
            throw new NotFoundException("Jobrole not found!");
        }

        foreach (var userId in userIds)
        {
            var user = await this._dbContext.Users.Include(x => x.JobRoles).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new NotFoundException("User not found!");
            }

            // Check if user doesnt already have the role
            if (user.JobRoles.FirstOrDefault(x => x.JobRoleId == jobRoleToModify.Id) == null)
            {
                user.JobRoles.Add(new UserJobRoles()
                {
                    JobRole = jobRoleToModify,
                    JobRoleId = jobRoleToModify.Id,
                    User = user,
                    UserId = (int)user.Id
                });
            }
        }

        await this._dbContext.SaveChangesAsync();
    }

    public async Task RemoveUsersFromJobRoleAsync(int id, List<long> userIds)
    {
        var jobRoleToModify = await this._dbContext.JobRoles.FirstOrDefaultAsync(jr => jr.Id == id);

        if (jobRoleToModify == null)
        {
            throw new NotFoundException("Jobrole not found!");
        }
        
        var schedulesWithUsers = this._dbContext.ShiftAssignments
            .Include(x => x.WorkSchedule)
            .Include(x => x.UserJobRole)
            .ThenInclude(x => x.User)
            .Where(x => userIds.Contains(x.UserJobRole.UserId))
            .ToList()
            .Select(x => x.WorkSchedule);

        foreach (var userId in userIds)
        {
            var user = await this._dbContext.Users.Include(x => x.JobRoles).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new NotFoundException("User not found!");
            }

            var jobRoleToRemove = user.JobRoles.FirstOrDefault(x => x.JobRoleId == jobRoleToModify.Id);
            // Check if user has the role
            if (jobRoleToRemove != null)
            {
                user.JobRoles.Remove(jobRoleToRemove);
            }
        }

        await this._dbContext.SaveChangesAsync();

            
        // I dont think removing the shifts here makes sense
        // If its invalid he has to regenerate it anyway
        //this._workScheduleService.RemoveAllShiftAssignments()

        foreach (var workSchedule in schedulesWithUsers)
        {
            await this._workScheduleService.SetScheduleOfflineAsync(workSchedule.Id);
            await this._workScheduleService.SetScheduleAsInvalidAsync(workSchedule.Id);
        }
    }

    public async Task<JobRoleDto> GetJobRoleAsync(int id)
    {
        var jobRole = await this._dbContext.JobRoles
            .Include(x => x.Prerequisites)
            .Include(x => x.Dependencies)
            .Include(x => x.UsersWithRole)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(jr => jr.Id == id);

        if (jobRole == null)
        {
            throw new NotFoundException("Jobrole not found!");
        }

        return this._mapper.Map<JobRole, JobRoleDto>(jobRole);
    }

    public async Task<QueryableJobRoleResponse> GetJobRolesAsync(PaginationDto paginationDto, string? searchString)
    {
        var jobRoles = this._dbContext.JobRoles
            .Include(x => x.Prerequisites)
            .Include(x => x.Dependencies)
            .Include(x => x.UsersWithRole)
            .ThenInclude(x => x.User)
            .OrderBy(x => x.Name)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            jobRoles = jobRoles.Where(x => x.Name.ToLower().Contains(searchString.ToLower()));
        }

        return new QueryableJobRoleResponse()
        {
            Count = jobRoles.Count(),
            JobRoles = jobRoles
                .Skip((paginationDto.Page - 1) * paginationDto.PageSize)
                .Take(paginationDto.PageSize)
                .Select(x => this._mapper.Map<JobRole, JobRoleShortDto>(x)).ToList()
        };
    }

    private async Task<bool> WouldCreateCycle(int roleId, int prerequisiteId)
    {
        // If you are trying to make a role dependent on itself
        if (roleId == prerequisiteId) return true;

        // Get the potential prerequisite and all its existing prerequisites (the chain)
        var prerequisite = await this._dbContext.JobRoles
            .Include(r => r.Prerequisites)
            .FirstOrDefaultAsync(r => r.Id == prerequisiteId);

        if (prerequisite == null) return false;

        // Recursively check if the current 'roleId' appears anywhere
        // in the prerequisite's own upstream chain
        foreach (var link in prerequisite.Prerequisites)
        {
            if (link.JobRoleId == roleId || await WouldCreateCycle(roleId, link.JobRoleId))
            {
                return true;
            }
        }

        return false;
    }
}
