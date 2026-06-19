using AutoMapper;
using Data;
using Data.Entities;
using Schichtpilot.Exceptions;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Services;

/// <summary>
/// Orchestrates company policy specific operations including getting, adding, removing public holidays and getting and setting the company policy.  
/// </summary>
public class CompanyPolicyService : ICompanyPolicyService
{
    private readonly SchichtpilotDbContext _dbContext;

    private readonly IMapper _mapper;

    private readonly IWorkScheduleService _workScheduleService;

    public CompanyPolicyService(SchichtpilotDbContext dbContext, IMapper mapper, IWorkScheduleService workScheduleService)
    {
        this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this._mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        this._workScheduleService = workScheduleService ?? throw new ArgumentNullException(nameof(workScheduleService));
    }

    /// <summary>
    /// Adds a new public holiday into the system.
    /// </summary>
    /// <param name="holidays"> The new holidays to be added. </param>
    /// <returns></returns>
    public async Task AddHolidaysAsync(HolidaysDto holidays)
    {
        foreach (var holiday in holidays.Holidays)
        {
            var holidayEntity = this._dbContext.Holidays.FirstOrDefault(x => x.Date.Date == holiday.Date.Date);

            if (holidayEntity == null)
            {
                this._dbContext.Holidays.Add(new Holiday
                {
                    Date = holiday.Date
                });
            }
        }

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Removes saved holidays from the system.
    /// </summary>
    /// <param name="holidays"> The holidays to be removed. </param>
    /// <returns></returns>
    public async Task RemoveHolidaysAsync(HolidaysDto holidays)
    {
        foreach (var holiday in holidays.Holidays)
        {
            var holidayEntity = this._dbContext.Holidays.FirstOrDefault(x => x.Date.Date == holiday.Date.Date);

            if (holidayEntity != null)
            {
                this._dbContext.Holidays.Remove(holidayEntity);
            }

        }

        await this._dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Gets all available holidays saved in the system.
    /// </summary>
    /// <returns> Returns the holidays as <see cref="HolidaysDto"/>. </returns>
    public Task<HolidaysDto> GetHolidaysAsync()
    {
        var holidays = this._dbContext.Holidays.ToList();
        return Task.FromResult(new HolidaysDto
        {
            Holidays = holidays.Select(x => x.Date.Date).ToList()
        });
    }

    /// <summary>
    /// Sets the company policy.
    /// </summary>
    /// <param name="policyDto"> The new company policy. </param>
    /// <returns></returns>
    public async Task SetPolicyAsync(CompanyPolicyDto policyDto)
    {
        var policy = this._dbContext.WorkPolicies.FirstOrDefault();

        if (policy == null)
        {
            this._dbContext.WorkPolicies.Add(new WorkPolicy
            {
                MaximumConsecutiveWorkHoursPerDay = policyDto.MaximumConsecutiveWorkHoursPerDay,
                MinimumRestPeriodInMinutes = policyDto.MinimumRestPeriodInMinutes,
                RestPeriodThresholdInMinutes = policyDto.RestPeriodThresholdInMinutes,
                MaximumConsecutiveWorkHoursPerWeek = policyDto.MaximumConsecutiveWorkHoursPerWeek
            });
            await this._dbContext.SaveChangesAsync();
            return;
        }

        policy.MaximumConsecutiveWorkHoursPerDay = policyDto.MaximumConsecutiveWorkHoursPerDay;
        policy.MinimumRestPeriodInMinutes = policyDto.MinimumRestPeriodInMinutes;
        policy.RestPeriodThresholdInMinutes = policyDto.RestPeriodThresholdInMinutes;
        policy.MaximumConsecutiveWorkHoursPerWeek = policyDto.MaximumConsecutiveWorkHoursPerWeek;
        await this._dbContext.SaveChangesAsync();
        var schedules = this._dbContext.WorkSchedules.ToList();
        foreach (var schedule in schedules)
        {
            await this._workScheduleService.SetScheduleOfflineAsync(schedule.Id);
            await this._workScheduleService.SetScheduleAsInvalidAsync(schedule.Id);
        }
    }

    /// <summary>
    /// Gets the currently set company policy.
    /// </summary>
    /// <returns> Returns the company policy as <see cref="CompanyPolicyDto"/>. </returns>
    /// <exception cref="NotSetException"> Thrown if the policy is not set. </exception>
    public Task<CompanyPolicyDto> GetPolicyAsync()
    {
        var policy = this._dbContext.WorkPolicies.FirstOrDefault();

        if (policy == null)
        {
            throw new NotSetException("Policy is not set");
        }

        return Task.FromResult(this._mapper.Map<CompanyPolicyDto>(policy));
    }
}