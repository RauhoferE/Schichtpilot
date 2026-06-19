using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;

namespace IntegrationTests.Fakes;

public class FakeCompanyPolicyService : ICompanyPolicyService
{
    private CompanyPolicyDto? _policy;
    private HolidaysDto? _holidays;

    public CompanyPolicyDto? LastSetPolicy => _policy;
    public HolidaysDto? LastHolidays => _holidays;

    public Task AddHolidaysAsync(HolidaysDto holidays)
    {
        _holidays = holidays;
        return Task.CompletedTask;
    }

    public Task RemoveHolidaysAsync(HolidaysDto holidays)
    {
        _holidays = holidays;
        return Task.CompletedTask;
    }

    public Task<HolidaysDto> GetHolidaysAsync()
    {
        return Task.FromResult(_holidays ?? new HolidaysDto { Holidays = new List<DateTime>() });
    }

    public Task SetPolicyAsync(CompanyPolicyDto policyDto)
    {
        _policy = policyDto;
        return Task.CompletedTask;
    }

    public Task<CompanyPolicyDto> GetPolicyAsync()
    {
        return Task.FromResult(_policy ?? new CompanyPolicyDto
        {
            MinimumRestPeriodInMinutes = 30,
            RestPeriodThresholdInMinutes = 60,
            MaximumConsecutiveWorkHoursPerDay = 8,
            MaximumConsecutiveWorkHoursPerWeek = 40
        });
    }
}
