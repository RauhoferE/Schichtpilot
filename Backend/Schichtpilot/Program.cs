using Scalar.AspNetCore;
using Schichtpilot.Configuration;
using Schichtpilot.Services;
namespace Schichtpilot;

public class Program
{
    
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        
        builder.Services.Configure<AzureEmailSettings>(
            builder.Configuration.GetSection(AzureEmailSettings.SectionName));
        
        // Email service 
        builder.Services.AddScoped<IEmailService, EmailService>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(); // ← adds the API UI
           // app.MapGet("/", () => Results.Redirect("/scalar/v1"));
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}