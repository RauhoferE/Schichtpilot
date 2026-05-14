using AutoMapper;
using Data;
using Data.Entities;
using Microsoft.EntityFrameworkCore;
using Schichtpilot.Exceptions;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Services;

/// <summary>
/// Orchestrates job role related operations including creating, updating, deleting job roles.
/// Also includes adding, removing users from jobs and managing job dependencies on other jobs.
/// </summary>
public class JobRoleService : IJobRoleService
{
    public JobRoleService(SchichtpilotDbContext dbContext, IMapper mapper, IWorkScheduleService workScheduleService, IShiftService shiftService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _workScheduleService = workScheduleService ?? throw new ArgumentNullException(nameof(workScheduleService));
        _shiftService = shiftService ?? throw new ArgumentNullException(nameof(shiftService));
    }

    private readonly SchichtpilotDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IWorkScheduleService _workScheduleService;
    private readonly IShiftService _shiftService;


    /// <summary>
    /// Creates a new job role.
    /// </summary>
    /// <param name="jobRole"> The new job role to be added. </param>
    /// <returns></returns>
    /// <exception cref="AlreadyExistsException"> Thrown when a job role with the exact name already exists. </exception>
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
    /// <summary>
    /// Updates an existing job role.
    /// </summary>
    /// <param name="id"> The job role to be updated. </param>
    /// <param name="jobRole"> The updated parameters of a job. </param>
    /// <returns></returns>
    /// <exception cref="AlreadyExistsException"> Thrown when a job role with the same name already exists. </exception>
    /// <exception cref="NotFoundException"> Thrown when the job role that should be updated cannot be found. </exception>
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


    /// <summary>
    /// Adds a new job dependency to an existing job. 
    /// </summary>
    /// <param name="jobRoleId"> The job role to be updated. </param>
    /// <param name="dependencyId"> The id of the job role to be added as a dependency. </param>
    /// <returns></returns>
    /// <exception cref="NotFoundException"> Thrown when the job role or the dependency could not be found. </exception>
    /// <exception cref="AlreadyExistsException"> Thrown when the job role already has the exact same dependency. </exception>
    /// <exception cref="InvalidDependencyException"> Thrown when the added dependency would create a cycle. </exception>
    public async Task AddDependenciesToJobRoleAsync(int jobRoleId, int dependencyId)
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
            throw new AlreadyExistsException("Dependency already exisits!");
        }

        if (await this.WouldCreateCycle(dependencyId, jobRoleId))
        {
            throw new InvalidDependencyException("Circular dependency detected!");
        }

        this._dbContext.JobRoleDependencies.Add(new JobRoleDependency()
        {
            
            DependencyJobRoleId = dependencyJobRole.Id,
            //Dependency = dependencyJobRole,
            //JobRole = jobRole,
            JobRoleId = jobRole.Id
        });

        await this._dbContext.SaveChangesAsync();

        var shiftsToModify = this._dbContext.ShiftRequirements
            .Include(x => x.JobRole)
            .Include(x => x.JobRole)
            .Where(x => x.JobRoleId == jobRole.Id)
            .Select(x => x.Shift);

        foreach (var shift in shiftsToModify)
        {
            await this._shiftService.AddJobRequirementAsync(shift.Id, new ShiftRequirementDto()
            {
                JobId = jobRole.Id,
                RequiredStaffCount = 1
            });
        }

        var schedulesWithRole = this._dbContext.ShiftAssignments
            .Include(x => x.WorkSchedule)
            .Include(x => x.UserJobRole)
            .ThenInclude(x => x.JobRole)
            .Where(x => x.UserJobRole.JobRoleId == jobRoleId)
            .ToList()
            .Select(x => x.WorkSchedule);

        foreach (var workSchedule in schedulesWithRole)
        {
            await this._workScheduleService.SetScheduleOfflineAsync(workSchedule.Id);
            await this._workScheduleService.SetScheduleAsInvalidAsync(workSchedule.Id);
        }
    }

    /// <summary>
    /// Removes an existing job role as a dependency from a job role.
    /// </summary>
    /// <param name="jobRoleId"> The job role that contains the dependency. </param>
    /// <param name="dependencyId"> The dependency to be removed. </param>
    /// <returns></returns>
    /// <exception cref="NotFoundException"> Thrown when the job role or the dependency could not be found. </exception>
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

    /// <summary>
    /// Adds an existing user to a job role.
    /// </summary>
    /// <param name="id"> The job role to be added to the user. </param>
    /// <param name="userId"> The user that gains the specific role. </param>
    /// <returns></returns>
    /// <exception cref="NotFoundException"> Thrown when the job role or dependency could not be found. </exception>
    public async Task AddUserToJobRoleAsync(int id, long userId)
    {
        var jobRoleToModify = await this._dbContext.JobRoles.FirstOrDefaultAsync(jr => jr.Id == id);

        if (jobRoleToModify == null)
        {
            throw new NotFoundException("Jobrole not found!");
        }
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

        await this._dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Removes a user from a job role. 
    /// </summary>
    /// <param name="id"> The job role to be removed from the user. </param>
    /// <param name="userId"> The user that losses the specific role. </param>
    /// <returns></returns>
    /// <exception cref="NotFoundException"> Thrown when the user could not be found. </exception>
    public async Task RemoveUserFromJobRoleAsync(int id, long userId)
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
            .Where(x => x.UserJobRole.UserId == userId)
            .ToList()
            .Select(x => x.WorkSchedule);

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

    /// <summary>
    /// Deletes an existing role.
    /// </summary>
    /// <param name="id"> The role to be deleted. </param>
    /// <returns></returns>
    /// <exception cref="NotFoundException"> Thrown when the job role could not be found. </exception>
    /// <exception cref="PolicyConflictException"> Thrown when the job role is still used in a shift. </exception>
    public async Task DeleteRoleAsync(int id)
    {
        var jobRoleToModify = await this._dbContext.JobRoles
            .Include(x => x.UsersWithRole)
            .Include(x => x.Dependencies)
            .Include(x => x.Dependencies)
            .FirstOrDefaultAsync(jr => jr.Id == id);

        if (jobRoleToModify == null)
        {
            throw new NotFoundException("Jobrole not found!");
        }

        var usedInShifts = this._dbContext.ShiftRequirements
            .Include(x => x.JobRole)
            .Include(x => x.JobRole)
            .Any(x => x.JobRoleId == jobRoleToModify.Id);

        if (usedInShifts)
        {
            throw new PolicyConflictException("Jobrole still active in shift!");
        }

        this._dbContext.JobRoles.Remove(jobRoleToModify);
        await this._dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the details of a job role. 
    /// </summary>
    /// <param name="id"> The targeted jobrole. </param>
    /// <returns> Returns the job role as <see cref="JobRoleDto"/>. </returns>
    /// <exception cref="NotFoundException"> Thrown when the job role could not be found. </exception>
    public async Task<JobRoleDto> GetJobRoleAsync(int id)
    {
        var jobRole = await this._dbContext.JobRoles
            .Include(x => x.Prerequisites)
            .ThenInclude(x => x.JobRole)
            .Include(x => x.Dependencies)
            .ThenInclude(x => x.Dependency)
            .Include(x => x.UsersWithRole)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(jr => jr.Id == id);

        if (jobRole == null)
        {
            throw new NotFoundException("Jobrole not found!");
        }

        var mapped = this._mapper.Map<JobRole, JobRoleDto>(jobRole);
        return mapped;
    }

    /// <summary>
    /// Gets a list of job roles.
    /// </summary>
    /// <param name="paginationDto"> The pagination element. </param>
    /// <param name="searchString"> The job role to look for. </param>
    /// <returns> Returns the job roles as <see cref="QueryableJobRoleResponse"/>. </returns>
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

    /// <summary>
    /// Checks if the job role appears anywhere in the prerequisiste job role dependency chain.
    /// </summary>
    /// <param name="roleId"> The dependent job role. </param>
    /// <param name="prerequisiteId"> The prerequisite job role. </param>
    /// <returns> Returns true if the job role would create a cycle. </returns>
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
