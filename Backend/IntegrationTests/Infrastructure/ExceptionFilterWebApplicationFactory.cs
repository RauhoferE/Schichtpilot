using Data;
using IntegrationTests.Fakes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Schichtpilot.Interfaces;

namespace IntegrationTests.Infrastructure;

public class ExceptionFilterWebApplicationFactory : WebApplicationFactory<Schichtpilot.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuthCookieName"] = "IntegrationTestsAuthCookie",
                ["AllowedCors:0"] = "http://localhost",
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=(localdb)\\mssqllocaldb;Database=SchichtpilotIntegrationTests;Trusted_Connection=True;MultipleActiveResultSets=true",
                ["AzureEmail:ConnectionString"] = "endpoint=https://example.com/;accesskey=fake",
                ["AzureEmail:SenderAddress"] = "no-reply@example.com",
                ["AzureEmail:SendMail"] = "false",
                ["AuthenticationSettings:JwtKey"] = "integration-tests-jwt-key-1234567890",
                ["AuthenticationSettings:TokenLifeTimeInMinutes"] = "60"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<SchichtpilotDbContext>));
            services.AddDbContext<SchichtpilotDbContext>(options =>
                options.UseInMemoryDatabase("IntegrationTestsDb"));

            services.RemoveAll(typeof(IAuthService));
            services.RemoveAll(typeof(IUserService));
            services.RemoveAll(typeof(ICompanyPolicyService));
            services.RemoveAll(typeof(IWorkScheduleService));
            services.RemoveAll(typeof(IAbsenceService));
            services.RemoveAll(typeof(IShiftService));
            services.RemoveAll(typeof(IJobRoleService));
            services.RemoveAll(typeof(IEmailService));

            services.AddSingleton<IAuthService, FakeAuthService>();
            services.AddSingleton<IUserService, FakeUserService>();
            services.AddSingleton<ICompanyPolicyService, FakeCompanyPolicyService>();
            services.AddSingleton<IWorkScheduleService, FakeWorkScheduleService>();
            services.AddSingleton<IAbsenceService, ThrowingAbsenceService>();
            services.AddSingleton<IShiftService, FakeShiftService>();
            services.AddSingleton<IJobRoleService, FakeJobRoleService>();
            services.AddSingleton<IEmailService, FakeEmailService>();

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });

            services.AddAuthorization();
        });
    }
}
