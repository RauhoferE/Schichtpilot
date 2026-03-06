namespace Schichtpilot.Interfaces;

public interface ICompanyPolicyService
{
    Task SetHolidaysAsync();
    Task RemoveHolidaysAsync();
    Task SetMaximumConsecutiveWorkHoursAsync();

    Task SetRequiredRestPeriodAsync();
}