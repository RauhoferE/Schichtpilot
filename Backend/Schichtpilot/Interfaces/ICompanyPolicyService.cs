namespace Schichtpilot.Interfaces;

public interface ICompanyPolicyService
{
    Task SetHolidays();
    Task RemoveHolidays();
    Task SetMaximumConsecutiveWorkHours();
}