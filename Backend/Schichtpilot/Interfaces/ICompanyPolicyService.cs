using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Interfaces;


/// <summary>
/// Orchestrates company policy specific operations including getting, adding, removing public holidays and getting and setting the company policy.  
/// </summary>
public interface ICompanyPolicyService
{
    /// <summary>
    /// Adds a new public holiday into the system.
    /// </summary>
    /// <param name="holidays"> The new holidays to be added. </param>
    /// <returns></returns>
    Task AddHolidaysAsync(HolidaysDto holidays);

    /// <summary>
    /// Removes saved holidays from the system.
    /// </summary>
    /// <param name="holidays"> The holidays to be removed. </param>
    /// <returns></returns>
    Task RemoveHolidaysAsync(HolidaysDto holidays);


    /// <summary>
    /// Gets all available holidays saved in the system.
    /// </summary>
    /// <returns> Returns the holidays as <see cref="HolidaysDto"/>. </returns>
    Task<HolidaysDto> GetHolidaysAsync();

    /// <summary>
    /// Sets the company policy.
    /// </summary>
    /// <param name="policyDto"> The new company policy. </param>
    /// <returns></returns>
    Task SetPolicyAsync(CompanyPolicyDto policyDto);

    /// <summary>
    /// Gets the currently set company policy.
    /// </summary>
    /// <returns> Returns the company policy as <see cref="CompanyPolicyDto"/>. </returns>
    Task<CompanyPolicyDto> GetPolicyAsync();
}