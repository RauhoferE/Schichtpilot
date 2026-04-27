using Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IntegrationTests.Infrastructure;

public class ProgramServicesWebApplicationFactory : WebApplicationFactory<Schichtpilot.Program>
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
        });
    }
}
