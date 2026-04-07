using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Middleware;
using Backend.Services.Interfaces;
using Backend.Services.Implementations;
using Backend.Models.DTOs;
using Backend.Validation;
using dotenv.net;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using System.Text;
using AspNetCoreRateLimit;

// Load .env file - try from current directory, ignore if not found
try
{
    DotEnv.Load();
}
catch
{
    // .env file might not exist in container, environment variables should be set by docker-compose
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Rate Limiting Configuration
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimitOptions"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
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
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? "your-secret-key-change-this-in-production-min-16-chars";
var key = Encoding.ASCII.GetBytes(secretKey);

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
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
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

var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Database connection string is missing or empty. Set the CONNECTION_STRING environment variable.");
}

builder.WebHost.UseUrls("http://0.0.0.0:8080");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IChecklistService, ChecklistService>();
builder.Services.AddScoped<ILabelService, LabelService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITaskWatcherService, TaskWatcherService>();
builder.Services.AddScoped<ITaskAttachmentService, TaskAttachmentService>();
builder.Services.AddScoped<IValidator<CreateTaskDto>, CreateTaskDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateTaskStatusDto>, UpdateTaskStatusDtoValidator>();
builder.Services.AddScoped<IValidator<AssignTaskDto>, AssignTaskDtoValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "JWT Authentication",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token"
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAllOrigins");

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseIpRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        if (DatabaseHasExistingSchema(db))
        {
            Console.WriteLine("⚠️ Database schema already exists, skipping initial migration");
        }
        else
        {
            db.Database.Migrate();
            Console.WriteLine("✅ Database migrated successfully");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database migration failed: {ex.Message}");
        throw; // Re-throw to fail fast in container
    }
}

static bool DatabaseHasExistingSchema(AppDbContext db)
{
    db.Database.OpenConnection();

    try
    {
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'Projects')";
        var result = command.ExecuteScalar();
        return result is bool exists && exists;
    }
    finally
    {
        db.Database.CloseConnection();
    }
}

app.Run();
