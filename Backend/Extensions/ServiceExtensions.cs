using Backend.Data;
using Backend.Middleware;
using Backend.Services.Interfaces;
using Backend.Services.Implementations;
using Backend.Models.DTOs;
using Backend.Validation;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using System.Text;
using AspNetCoreRateLimit;
using Microsoft.Extensions.Options;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Backend.Extensions;

public static class ServiceExtensions
{
    public static void AddCorsConfig(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAllOrigins", policy =>
            {
                policy.AllowAnyOrigin()
                      .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                      .AllowAnyHeader();
            });
        });
    }

    public static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["CONNECTION_STRING"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection string is missing or empty. Ensure the CONNECTION_STRING environment variable is set.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
    }

    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IChecklistService, ChecklistService>();
        services.AddScoped<ILabelService, LabelService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ITaskWatcherService, TaskWatcherService>();
        services.AddScoped<ITaskAttachmentService, TaskAttachmentService>();
        services.AddScoped<DataSeeder>();

        services.AddScoped<IValidator<CreateTaskDto>, CreateTaskDtoValidator>();
        services.AddScoped<IValidator<UpdateTaskStatusDto>, UpdateTaskStatusDtoValidator>();
        services.AddScoped<IValidator<AssignTaskDto>, AssignTaskDtoValidator>();
    }

    public static void AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimitOptions"));
        services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        services.AddHttpContextAccessor();
    }

    public static void AddApiVersioningConfig(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'V";
            options.SubstituteApiVersionInUrl = true;
        });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

                var response = ApiResponseDto<object>.Fail("Validation failed", errors);
                return new BadRequestObjectResult(response);
            };
        });

        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();
        // Uses the current assembly
        services.AddValidatorsFromAssemblyContaining<Program>();
    }

    public static void AddAuthAndAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"];
        var key = Encoding.ASCII.GetBytes(secretKey!);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("TaskRead", policy =>
                policy.RequireRole("Admin", "Manager", "User", "Viewer"));

            options.AddPolicy("TaskWrite", policy =>
                policy.RequireRole("Admin", "Manager", "User"));

            options.AddPolicy("ProjectRead", policy =>
                policy.RequireRole("Admin", "Manager", "User", "Viewer"));

            options.AddPolicy("ProjectWrite", policy =>
                policy.RequireRole("Admin", "Manager", "User"));
        });
    }

    public static void AddSwaggerConfig(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Task Flow API",
                Version = "v1"
            });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "JWT Authentication",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token"
            };

            options.AddSecurityDefinition("Bearer", securityScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            });
        });
    }

    public static void AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString =
            Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
            ?? configuration.GetConnectionString("Redis")
            ?? configuration["Redis:ConnectionString"];

        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = configuration["Redis:InstanceName"] ?? "GDGHackathon:";
            });
        }
    }
}
