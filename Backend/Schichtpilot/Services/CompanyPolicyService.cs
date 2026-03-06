using AutoMapper;
using Data;
using Data.Entities;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Services;

public class CompanyPolicyService : ICompanyPolicyService
{
    private readonly SchichtpilotDbContext _dbContext;
    
    private readonly IMapper _mapper;

    public CompanyPolicyService(SchichtpilotDbContext dbContext, IMapper mapper)
    {
        this._dbContext = dbContext ??  throw new ArgumentNullException(nameof(dbContext));
        this._mapper = mapper ??  throw new ArgumentNullException(nameof(mapper));
    }

    public async Task AddHolidaysAsync(HolidaysDto holidays)
    {
        foreach (var holiday in holidays.Holidays)
        {
            var holidayEntity = this._dbContext.Holidays.FirstOrDefault(x => x.Date.Date == holiday.Date.Date);

            if (holidayEntity == null)
            {
                this._dbContext.Holidays.Add(new  Holiday()
                {
                    Date = holiday.Date
                });
            }
        }
        
        await _dbContext.SaveChangesAsync();
    }

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

    public Task<HolidaysDto> GetHolidaysAsync()
    {
        var holidays = this._dbContext.Holidays.ToList();
        return Task.FromResult(new HolidaysDto()
        {
            Holidays = holidays.Select(x => x.Date.Date).ToList()
        });
    }

    public async Task SetPolicyAsync(CompanyPolicyDto policyDto)
    {
        var policy = this._dbContext.WorkPolicies.FirstOrDefault();

        if (policy == null)
        {
            this._dbContext.WorkPolicies.Add(new WorkPolicy()
            {
                MaximumConsecutiveWorkHours = policyDto.MaximumConsecutiveWorkHours,
                RestPeriodInMinutes = policyDto.RestPeriodInMinutes,
                RestPeriodThresholdInMinutes = policyDto.RestPeriodThresholdInMinutes
            });
            await this._dbContext.SaveChangesAsync();
            return;
        }
        
        policy.MaximumConsecutiveWorkHours = policyDto.MaximumConsecutiveWorkHours;
        policy.RestPeriodInMinutes = policyDto.RestPeriodInMinutes;
        policy.RestPeriodThresholdInMinutes = policyDto.RestPeriodThresholdInMinutes;
        await this._dbContext.SaveChangesAsync();
    }

    public Task<CompanyPolicyDto> GetPolicyAsync()
    {
        var policy = this._dbContext.WorkPolicies.FirstOrDefault();
        return Task.FromResult(this._mapper.Map<CompanyPolicyDto>(policy));
    }
}