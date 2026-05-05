using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IntegrationTests.Fakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Schichtpilot.Interfaces;

namespace IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    protected CustomWebApplicationFactory Factory { get; }
    protected HttpClient Client { get; }

    protected T GetService<T>() where T : notnull
    {
        using var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    protected FakeAuthService AuthService => (FakeAuthService)GetService<IAuthService>();
    protected FakeUserService UserService => (FakeUserService)GetService<IUserService>();
    protected FakeAbsenceService AbsenceService => (FakeAbsenceService)GetService<IAbsenceService>();
    protected FakeShiftService ShiftService => (FakeShiftService)GetService<IShiftService>();
    protected FakeCompanyPolicyService CompanyPolicyService => (FakeCompanyPolicyService)GetService<ICompanyPolicyService>();
    protected FakeJobRoleService JobRoleService => (FakeJobRoleService)GetService<IJobRoleService>();
    protected FakeWorkScheduleService WorkScheduleService => (FakeWorkScheduleService)GetService<IWorkScheduleService>();
    protected FakeEmailService EmailService => (FakeEmailService)GetService<IEmailService>();

    protected static StringContent JsonContent(object payload)
    {
        return new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    }

    protected static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<T>();
    }
}
