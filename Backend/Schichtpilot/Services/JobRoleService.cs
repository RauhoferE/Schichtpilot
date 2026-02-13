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
    public JobRoleService(SchichtpilotDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    private readonly SchichtpilotDbContext _dbContext;
    private readonly IMapper _mapper;
    
    
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
            var dependentJobRole = this._dbContext.JobRoles.FirstOrDefault(jr => jr.Id == jobRoleIds);
            if (dependentJobRole != null)
            {
                this._dbContext.JobRoleDependencies.Add(new JobRoleDependency()
                {
                    Dependency = dependentJobRole,
                    JobRole = createdJobRole
                });
            }
        }
    }

    public Task UpdateJobRoleAsync(int id, EditJobRoleDto jobRole)
    {
        throw new NotImplementedException();
    }

    public Task AddDependenciesToJobRole(int jobRoleId, int dependencyId)
    {
        throw new NotImplementedException();
    }

    public Task RemoveDependenciesToJobRole(int jobRoleId, int dependencyId)
    {
        throw new NotImplementedException();
    }

    public Task AddUsersToJobRoleAsync(int id, List<int> userIds)
    {
        throw new NotImplementedException();
    }

    public Task RemoveUsersFromJobRoleAsync(int id, List<int> userIds)
    {
        throw new NotImplementedException();
    }

    public Task<JobRoleDto> GetJobRoleAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<QueryableJobRoleResponse> GetJobRolesAsync(PaginationDto paginationDto)
    {
        throw new NotImplementedException();
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