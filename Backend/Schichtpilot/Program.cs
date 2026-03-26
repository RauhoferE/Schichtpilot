using System.Reflection;
using AutoMapper;
using Data;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Schichtpilot.Interfaces;
using Schichtpilot.Mapping;
using Schichtpilot.Middleware;
using Schichtpilot.Services;
using Serilog;

namespace Schichtpilot;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true, true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", false, true)
            .AddEnvironmentVariables();
        
        //Serilog
        builder.Host.UseSerilog((_, loggerconfiguration) =>
        {
            loggerconfiguration.ReadFrom.Configuration(builder.Configuration);
        });
        
        var config = configuration.Build();
        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(config).CreateLogger();
        
        // Database
        builder.Services.AddDbContext<SchichtpilotDbContext>(options => options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        // Add services to the container.
        builder.Services.AddTransient<IEmailService, EmailService>();
        builder.Services.AddTransient<IUserService, UserService>();
        builder.Services.AddTransient<ICompanyPolicyService, CompanyPolicyService>();
        builder.Services.AddTransient<IWorkScheduleService, WorkScheduleService>();
        builder.Services.AddTransient<IAbsenceService, AbsenceService>();
        builder.Services.AddTransient<IAuthService, AuthService>();
        builder.Services.AddTransient<IShiftService, ShiftService>();
        builder.Services.AddTransient<IJobRoleService, JobRoleService>();

        // Validation
        builder.Services.AddMvc(options =>
        {
            options.Filters.Add<ValidationFilter>();
        });
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        builder.Services.AddFluentValidationAutoValidation();

        // Automapper
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new DtoMappingProfile());
        }, new NullLoggerFactory());
        
        IMapper mapper = mapperConfig.CreateMapper();
        builder.Services.AddSingleton(mapper);

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddControllers(options =>
        {
            options.Filters.Add<ExceptionFilter>();
        });
        
        // CORS
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("DevCors", builder =>
                {
                    builder.WithOrigins("http://localhost:4200");
                    builder.AllowAnyHeader();
                    builder.AllowAnyMethod();
                    builder.AllowCredentials();
                });
            });
        }

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        if (app.Environment.IsDevelopment())
        {
            app.UseCors("DevCors");
        }
        

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}