using System.Reflection;
using AutoMapper;
using Data;
using Data.Entities;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
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
        
        // Authentication
        var authCookieName = config["AuthCookieName"] ?? throw new Exception("AuthCookieName configuration is missing.");
        
        builder.Services.AddIdentity<User, IdentityRole<long>>(opt =>
            {
                opt.Password.RequireDigit = true;
                opt.Password.RequiredLength = 8;
                opt.Password.RequireNonAlphanumeric = true;
                opt.Password.RequireUppercase = true;
                opt.Password.RequireLowercase = true;
                opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(60);
                opt.Lockout.MaxFailedAccessAttempts = 5;
                opt.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<SchichtpilotDbContext>();
        
        builder.Services.ConfigureApplicationCookie(options =>
        {
            // Stop the cookie from redirecting to a Login Page
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
    
            // Stop the cookie from redirecting to an Access Denied Page
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };

            options.Cookie.Name = authCookieName;
            options.Cookie.HttpOnly = true; // Security: Prevents JS from reading the cookie
            options.Cookie.SameSite = SameSiteMode.Strict;
            if (builder.Environment.IsDevelopment())
            {
                options.Cookie.SameSite = SameSiteMode.Lax;
            }
        });

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
        var cors = config.GetSection("AllowedCors").Get<string[]>() ?? throw new Exception("AllowedCors configuration is missing.");
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("DevCors", builder =>
                {
                    builder.WithOrigins(cors);
                    builder.AllowAnyHeader();
                    builder.AllowAnyMethod();
                    builder.AllowCredentials();
                });
            });
        }
        
        // Swagger
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            { 
                Title = "Schichtpilot", 
                Version = "v1" 
            });

            // 2. Add the definition to Swagger
            options.AddSecurityDefinition("CookieAuth", new OpenApiSecurityScheme
            {
                Name = authCookieName,
                In = ParameterLocation.Cookie,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "CookieAuth",
                Description = "Cookie-based authentication"
            });

            // 2. Make it global so every protected endpoint shows a lock icon
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("CookieAuth", document)] = []
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        if (app.Environment.IsDevelopment())
        {
            app.UseCors("DevCors");
        }
        
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}