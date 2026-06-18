using Data;
using Data.Entities;
using Microsoft.AspNetCore.Identity;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Services;

//TODO: Remove before prod
public class TestDataService : ITestDataService
{
    public TestDataService(SchichtpilotDbContext dbContext, IUserService userService, IJobRoleService jobRoleService,
        ICompanyPolicyService companyPolicyService, UserManager<User> userManager)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _jobRoleService = jobRoleService ?? throw new ArgumentNullException(nameof(jobRoleService));
        _companyPolicyService = companyPolicyService ?? throw new ArgumentNullException(nameof(companyPolicyService));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    private readonly SchichtpilotDbContext _dbContext;
    
    private readonly IUserService _userService;
    
    private readonly IJobRoleService _jobRoleService;
    
    private readonly ICompanyPolicyService _companyPolicyService;
    
    private readonly UserManager<User> _userManager;

    public async Task CreateUsersAsync(int employees, int managers)
    {
        if (this._dbContext.Users.FirstOrDefault(x => x.Email == "user0@company.com") != null)
        {
            return;
        }
        
        for (int i = 0; i < employees; i++)
        {
            await this._userService.CreateUserAsync(new UserDto()
            {
                AddressDto = new AddressDto()
                {
                    City = "Vienna",
                    PostalCode = 1120,
                    Street = "Teststraße " + i,
                },
                Email = $"user{i}@company.com",
                FirstName = $"Mia{i}",
                LastName = $"Test{i}",
                AssignedJobRoles = new List<JobRoleShortDto>(),
                Birthdate = DateTime.Now.AddYears(-20),
            }, "PasswordPassword1!!!");
        }
        
        for (int i = 0; i < managers; i++)
        {
            await this._userService.CreateUserAsync(new UserDto()
            {
                AddressDto = new AddressDto()
                {
                    City = "Vienna",
                    PostalCode = 1120,
                    Street = "Teststraße " + i,
                },
                Email = $"manager{i}@company.com",
                FirstName = $"Hannes{i}",
                LastName = $"Test{i}",
                AssignedJobRoles = new List<JobRoleShortDto>(),
                Birthdate = DateTime.Now.AddYears(-20),
            }, "PasswordPassword1!!!");

            var addedUser = this._dbContext.Users.FirstOrDefault(x => x.Email == $"manager{i}@company.com");
            if (addedUser != null)
            {
                await this._userManager.AddToRoleAsync(addedUser, "ADMIN");
                await this._userManager.RemoveFromRoleAsync(addedUser, "USER");
            }
            
        }
    }

    public async Task CreateRolesAsync()
    {
        
        if (this._dbContext.JobRoles.FirstOrDefault(x => x.Name == "Waiter") != null)
        {
            return;
        }
        
        await this._jobRoleService.CreateJobRoleAsync(new CreateJobRoleDto()
        {
            Name = "Waiter",
            Description = "Waiter",
            DependentOnJobRoleIds = new List<int>()
        });
        
        await this._jobRoleService.CreateJobRoleAsync(new CreateJobRoleDto()
        {
            Name = "Cook",
            Description = "Cook",
            DependentOnJobRoleIds = new List<int>()
        });
        
        await this._jobRoleService.CreateJobRoleAsync(new CreateJobRoleDto()
        {
            Name = "Cookhelp",
            Description = "Cookhelp",
            DependentOnJobRoleIds = new List<int>()
        });
        
        await this._jobRoleService.CreateJobRoleAsync(new CreateJobRoleDto()
        {
            Name = "Greeter",
            Description = "Greeter",
            DependentOnJobRoleIds = new List<int>()
        });
    }

    public async Task CreateWorkPolicyAsync()
    {
        if (this._dbContext.WorkPolicies.FirstOrDefault() != null)
        {
            return;
        }
        
        await this._companyPolicyService.SetPolicyAsync(new CompanyPolicyDto()
        {
            RestPeriodThresholdInMinutes = 480,
            MinimumRestPeriodInMinutes = 60,
            MaximumConsecutiveWorkHoursPerDay = 8,
            MaximumConsecutiveWorkHoursPerWeek = 40
        });
    }
}