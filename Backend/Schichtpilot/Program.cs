using System.Reflection;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Data;
using Data.Entities;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Schichtpilot.Interfaces;
using Schichtpilot.Mapping;
using Schichtpilot.Middleware;
using Schichtpilot.Services;
using Schichtpilot.Settings;
using Serilog;

namespace Schichtpilot;

/// <summary>
/// The main class of the web app.
/// </summary>
public class Program
{
    /// <summary>
    /// The main entry point of the application
    /// Configures and builds the webapplication.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true, true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", false, true)
            .AddEnvironmentVariables();

        // Serilog
        builder.Host.UseSerilog((_, loggerconfiguration) =>
        {
            loggerconfiguration.ReadFrom.Configuration(builder.Configuration);
        });

        var config = configuration.Build();
        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(config).CreateLogger();

        // Database
        builder.Services.AddDbContext<SchichtpilotDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        // Authentication
        var authCookieName = config["AuthCookieName"]
            ?? throw new Exception("AuthCookieName configuration is missing.");

        var jwtKey = config["AuthenticationSettings:JwtKey"]
            ?? throw new Exception("JwtKey configuration is missing.");

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
            .AddEntityFrameworkStores<SchichtpilotDbContext>()
            .AddDefaultTokenProviders();

        // JWT als Default-Auth-Schema überschreiben
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    RoleClaimType = ClaimTypes.Role
                };

                // Token aus Cookie lesen statt Authorization-Header
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["SchichtpilotUser"];
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = authCookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = builder.Environment.IsDevelopment()
                ? SameSiteMode.None
                : SameSiteMode.Strict;

            options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;

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
        });

        // Add settings
        builder.Services.Configure<AuthenticationSettings>(
            config.GetSection("AuthenticationSettings"));

        //  AzureEmail settings
        builder.Services.Configure<AzureEmailSettings>(
            config.GetSection("AzureEmail"));

        // Add services to the container
        builder.Services.AddTransient<IEmailService, EmailService>();
        builder.Services.AddTransient<IUserService, UserService>();
        builder.Services.AddTransient<ICompanyPolicyService, CompanyPolicyService>();
        builder.Services.AddTransient<IWorkScheduleService, WorkScheduleService>();
        builder.Services.AddTransient<IAbsenceService, AbsenceService>();
        builder.Services.AddTransient<IAuthService, AuthService>();
        builder.Services.AddTransient<IShiftService, ShiftService>();
        builder.Services.AddTransient<IJobRoleService, JobRoleService>();
        builder.Services.AddTransient<ITestDataService, TestDataService>();

        // Validation
        builder.Services.AddMvc(options =>
        {
            options.Filters.Add<ValidationFilter>();
        });

        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        builder.Services.AddFluentValidationAutoValidation();

        // AutoMapper
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
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        // CORS
        var cors = config.GetSection("AllowedCors").Get<string[]>()
            ?? throw new Exception("AllowedCors configuration is missing.");

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("DevCors", policy =>
                {
                    policy.WithOrigins(cors)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
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

            options.AddSecurityDefinition("CookieAuth", new OpenApiSecurityScheme
            {
                Name = authCookieName,
                In = ParameterLocation.Cookie,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "CookieAuth",
                Description = "Cookie-based authentication"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("CookieAuth", document)] = []
            });
        });
        
        var createTestData = config.GetSection("CreateTestData").Get<bool>();

        var app = builder.Build();
        
        if (createTestData)
        {
            using (var scope = app.Services.CreateScope())
            {
                var testDataService = scope.ServiceProvider.GetRequiredService<ITestDataService>();
                await testDataService.CreateUsersAsync(3,1);
                await testDataService.CreateRolesAsync();
                await testDataService.CreateWorkPolicyAsync();
            }

        }

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseSerilogRequestLogging();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseCors("DevCors");
        }
        
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<UserContextMiddleware>();

        app.MapControllers();

        app.Run();
    }
}