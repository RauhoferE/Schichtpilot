using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Interfaces;

public interface ICompanyPolicyService
{
    Task AddHolidaysAsync(HolidaysDto holidays);

    Task RemoveHolidaysAsync(HolidaysDto holidays);

    Task<HolidaysDto> GetHolidaysAsync();

    Task SetPolicyAsync(CompanyPolicyDto policyDto);

    Task<CompanyPolicyDto> GetPolicyAsync();
}