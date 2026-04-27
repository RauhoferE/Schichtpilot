using IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Schichtpilot.Interfaces;
using Xunit;

namespace IntegrationTests;

public class ServiceRegistrationTests
{
    [Fact]
    public void Program_Registers_All_Services()
    {
        using var factory = new ProgramServicesWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        var provider = scope.ServiceProvider;

        var serviceTypes = new[]
        {
            typeof(IEmailService),
            typeof(IUserService),
            typeof(ICompanyPolicyService),
            typeof(IWorkScheduleService),
            typeof(IAbsenceService),
            typeof(IAuthService),
            typeof(IShiftService),
            typeof(IJobRoleService)
        };

        foreach (var serviceType in serviceTypes)
        {
            var service = provider.GetRequiredService(serviceType);
            Assert.NotNull(service);
        }
    }
}
