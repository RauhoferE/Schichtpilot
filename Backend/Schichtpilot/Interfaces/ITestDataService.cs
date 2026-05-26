namespace Schichtpilot.Interfaces;

public interface ITestDataService
{
    public Task CreateUsersAsync(int employees, int managers);
    
    public Task CreateRolesAsync();
    
    public Task CreateWorkPolicyAsync();
}