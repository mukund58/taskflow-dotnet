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
using Serilog;
using Microsoft.Extensions.Options;
using Npgsql;
using Asp.Versioning;

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

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.Configure<SeedingOptions>(builder.Configuration.GetSection("Seeding"));

var redisConnectionString =
    Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("Redis")
    ?? builder.Configuration["Redis:ConnectionString"];

if (string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = builder.Configuration["Redis:InstanceName"] ?? "GDGHackathon:";
    });
}

// Rate Limiting Configuration
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimitOptions"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers();
builder.Services
    .AddApiVersioning(options =>
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
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IChecklistService, ChecklistService>();
builder.Services.AddScoped<ILabelService, LabelService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITaskWatcherService, TaskWatcherService>();
builder.Services.AddScoped<ITaskAttachmentService, TaskAttachmentService>();
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<IValidator<CreateTaskDto>, CreateTaskDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateTaskStatusDto>, UpdateTaskStatusDtoValidator>();
builder.Services.AddScoped<IValidator<AssignTaskDto>, AssignTaskDtoValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GDG Hackathon API",
        Version = "v1"
    });

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

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "GDG Hackathon API v1");
});

app.UseCors("AllowAllOrigins");

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseIpRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var seedingOptions = scope.ServiceProvider.GetRequiredService<IOptions<SeedingOptions>>().Value;

    try
    {
        var hasProjectTable = DatabaseTableExists(db, "Projects");

        if (!hasProjectTable)
        {
            db.Database.Migrate();
            app.Logger.LogInformation("Database migrated successfully (fresh schema)");
        }
        else
        {
            var alignedCount = AlignMigrationHistoryWithExistingSchema(db, app.Logger);

            if (alignedCount > 0)
            {
                app.Logger.LogInformation(
                    "Aligned {AlignedMigrationCount} migration history entries to existing schema",
                    alignedCount);
            }

            var pendingMigrations = (await db.Database.GetPendingMigrationsAsync()).ToList();

            if (pendingMigrations.Count > 0)
            {
                try
                {
                    db.Database.Migrate();
                    app.Logger.LogInformation(
                        "Applied {PendingMigrationCount} pending migrations",
                        pendingMigrations.Count);
                }
                catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.DuplicateTable)
                {
                    app.Logger.LogError(
                        ex,
                        "Migration failed with duplicate table while applying: {PendingMigrations}",
                        string.Join(", ", pendingMigrations));

                    throw new InvalidOperationException(
                        "Database schema and migration history are inconsistent. Align __EFMigrationsHistory with existing schema and retry.",
                        ex);
                }
            }
            else
            {
                app.Logger.LogInformation("No pending migrations found");
            }
        }

        var repairedSchemaItems = RepairCriticalSchemaDrift(db, app.Logger);
        if (repairedSchemaItems > 0)
        {
            app.Logger.LogWarning(
                "Applied {RepairedSchemaItems} schema repair operation(s) after migration reconciliation",
                repairedSchemaItems);
        }

        if (seedingOptions.Enabled)
        {
            var dataSeeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
            await dataSeeder.SeedAsync(seedingOptions);
        }
        else
        {
            app.Logger.LogInformation("Database seeding is disabled");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex, "Database migration failed");
        throw; // Re-throw to fail fast in container
    }
}

static int AlignMigrationHistoryWithExistingSchema(AppDbContext db, Microsoft.Extensions.Logging.ILogger logger)
{
    EnsureMigrationHistoryTable(db);

    var appliedMigrations = GetAppliedMigrationIds(db);
    var alignedCount = 0;

    const string efProductVersion = "8.0.8";

    var migrationBaselines = new (string MigrationId, Func<bool> IsAlreadyAppliedBySchema)[]
    {
        (
            "20260405000000_InitialCreate",
            () =>
                DatabaseTableExists(db, "Projects") &&
                DatabaseTableExists(db, "Users") &&
                DatabaseTableExists(db, "Tasks")
        ),
        (
            "20260405130000_AddDueDateToTasks",
            () => DatabaseColumnExists(db, "Tasks", "DueDate")
        ),
        (
            "20260407000000_AddTaskComments",
            () => DatabaseTableExists(db, "Comments")
        ),
        (
            "20260407001000_AddTaskChecklist",
            () => DatabaseTableExists(db, "ChecklistItems")
        ),
        (
            "20260407002000_AddCollaborationFeatures",
            () =>
                DatabaseTableExists(db, "Labels") &&
                DatabaseTableExists(db, "TaskLabels") &&
                DatabaseTableExists(db, "Attachments") &&
                DatabaseTableExists(db, "TaskWatchers") &&
                DatabaseTableExists(db, "Notifications")
        ),
        (
            "20260407084259_AddTaskChecklistAndActivity",
            () => DatabaseTableExists(db, "TaskActivities")
        ),
        (
            "20260408120000_AddProjectLevelAccessControl",
            () => DatabaseColumnExists(db, "Projects", "OwnerUserId")
        ),
        (
            "20260408153000_AddProjectMembershipAndInvitations",
            () =>
                DatabaseTableExists(db, "ProjectMembers") &&
                DatabaseTableExists(db, "ProjectInvitations")
        ),
        (
            "20260409130000_AddDueDateToProjects",
            () => DatabaseColumnExists(db, "Projects", "DueDate")
        ),
    };

    foreach (var migration in migrationBaselines)
    {
        if (appliedMigrations.Contains(migration.MigrationId))
            continue;

        if (!migration.IsAlreadyAppliedBySchema())
            continue;

        InsertMigrationHistoryRow(db, migration.MigrationId, efProductVersion);
        appliedMigrations.Add(migration.MigrationId);
        alignedCount++;

        logger.LogInformation(
            "Backfilled migration history for {MigrationId} based on existing schema",
            migration.MigrationId);
    }

    return alignedCount;
}

static void EnsureMigrationHistoryTable(AppDbContext db)
{
    db.Database.OpenConnection();

    try
    {
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                ""MigrationId"" character varying(150) NOT NULL,
                ""ProductVersion"" character varying(32) NOT NULL,
                CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
            );";

        command.ExecuteNonQuery();
    }
    finally
    {
        db.Database.CloseConnection();
    }
}

static HashSet<string> GetAppliedMigrationIds(AppDbContext db)
{
    if (!DatabaseTableExists(db, "__EFMigrationsHistory"))
        return new HashSet<string>(StringComparer.Ordinal);

    db.Database.OpenConnection();

    try
    {
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\"";

        using var reader = command.ExecuteReader();
        var applied = new HashSet<string>(StringComparer.Ordinal);

        while (reader.Read())
        {
            applied.Add(reader.GetString(0));
        }

        return applied;
    }
    finally
    {
        db.Database.CloseConnection();
    }
}

static void InsertMigrationHistoryRow(AppDbContext db, string migrationId, string productVersion)
{
    db.Database.OpenConnection();

    try
    {
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = @"
            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
            VALUES (@migrationId, @productVersion)
            ON CONFLICT (""MigrationId"") DO NOTHING;";

        var migrationParameter = command.CreateParameter();
        migrationParameter.ParameterName = "@migrationId";
        migrationParameter.Value = migrationId;
        command.Parameters.Add(migrationParameter);

        var productVersionParameter = command.CreateParameter();
        productVersionParameter.ParameterName = "@productVersion";
        productVersionParameter.Value = productVersion;
        command.Parameters.Add(productVersionParameter);

        command.ExecuteNonQuery();
    }
    finally
    {
        db.Database.CloseConnection();
    }
}

static int RepairCriticalSchemaDrift(AppDbContext db, Microsoft.Extensions.Logging.ILogger logger)
{
    var repairedCount = 0;

    if (DatabaseTableExists(db, "Tasks") && !DatabaseColumnExists(db, "Tasks", "DueDate"))
    {
        EnsureDueDateColumnExistsForTasks(db);
        repairedCount++;
        logger.LogWarning("Schema drift detected: added missing DueDate column to Tasks table");
    }

    if (
        DatabaseTableExists(db, "Tasks") &&
        DatabaseColumnExists(db, "Tasks", "CreatedById") &&
        DatabaseColumnIsNotNullable(db, "Tasks", "CreatedById"))
    {
        EnsureLegacyCreatedByColumnIsNullableForTasks(db);
        repairedCount++;
        logger.LogWarning("Schema drift detected: relaxed NOT NULL constraint on legacy Tasks.CreatedById column");
    }

    if (DatabaseTableExists(db, "Projects") && !DatabaseColumnExists(db, "Projects", "DueDate"))
    {
        EnsureDueDateColumnExistsForProjects(db);
        repairedCount++;
        logger.LogWarning("Schema drift detected: added missing DueDate column to Projects table");
    }

    return repairedCount;
}

static void EnsureDueDateColumnExistsForTasks(AppDbContext db)
{
    db.Database.OpenConnection();

    try
    {
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = @"
DO $$
DECLARE table_record RECORD;
BEGIN
    FOR table_record IN
        SELECT table_schema, table_name
        FROM information_schema.tables
        WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
          AND lower(table_name) = 'tasks'
    LOOP
        EXECUTE format(
            'ALTER TABLE %I.%I ADD COLUMN IF NOT EXISTS %I timestamp with time zone',
            table_record.table_schema,
            table_record.table_name,
            'DueDate');
    END LOOP;
END $$;";

        command.ExecuteNonQuery();
    }
    finally
    {
        db.Database.CloseConnection();
    }
}

static void EnsureDueDateColumnExistsForProjects(AppDbContext db)
{
    db.Database.OpenConnection();

    try
    {
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = @"
DO $$
DECLARE table_record RECORD;
BEGIN
    FOR table_record IN
        SELECT table_schema, table_name
        FROM information_schema.tables
        WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
          AND lower(table_name) = 'projects'
    LOOP
        EXECUTE format(
            'ALTER TABLE %I.%I ADD COLUMN IF NOT EXISTS %I timestamp with time zone',
            table_record.table_schema,
            table_record.table_name,
            'DueDate');
    END LOOP;
END $$;";

        command.ExecuteNonQuery();
    }
    finally
    {
        db.Database.CloseConnection();
    }
}

static void EnsureLegacyCreatedByColumnIsNullableForTasks(AppDbContext db)
{
    db.Database.OpenConnection();

    try
    {
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = @"
DO $$
DECLARE column_record RECORD;
BEGIN
    FOR column_record IN
        SELECT table_schema, table_name, column_name
        FROM information_schema.columns
        WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
          AND lower(table_name) = 'tasks'
          AND lower(column_name) = 'createdbyid'
          AND is_nullable = 'NO'
    LOOP
        EXECUTE format(
            'ALTER TABLE %I.%I ALTER COLUMN %I DROP NOT NULL',
            column_record.table_schema,
            column_record.table_name,
            column_record.column_name);
    END LOOP;
END $$;";

        command.ExecuteNonQuery();
    }
    finally
    {
        db.Database.CloseConnection();
    }
}

static bool DatabaseTableExists(AppDbContext db, string tableName)
{
    db.Database.OpenConnection();

    try
    {
        using var command = db.Database.GetDbConnection().CreateCommand();

                command.CommandText = @"SELECT EXISTS (
            SELECT 1
            FROM information_schema.tables
                        WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
                            AND lower(table_name) = lower(@tableName)
        )";

        var tableNameParameter = command.CreateParameter();
        tableNameParameter.ParameterName = "@tableName";
        tableNameParameter.Value = tableName;
        command.Parameters.Add(tableNameParameter);

        var result = command.ExecuteScalar();
        return result is bool exists && exists;
    }
    finally
    {
        db.Database.CloseConnection();
    }
}

static bool DatabaseColumnExists(AppDbContext db, string tableName, string columnName)
{
    db.Database.OpenConnection();

    try
    {
        using var command = db.Database.GetDbConnection().CreateCommand();
                command.CommandText = @"SELECT EXISTS (
            SELECT 1
            FROM information_schema.columns
                        WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
                            AND lower(table_name) = lower(@tableName)
              AND lower(column_name) = lower(@columnName)
        )";

        var tableNameParameter = command.CreateParameter();
        tableNameParameter.ParameterName = "@tableName";
        tableNameParameter.Value = tableName;
        command.Parameters.Add(tableNameParameter);

        var columnNameParameter = command.CreateParameter();
        columnNameParameter.ParameterName = "@columnName";
        columnNameParameter.Value = columnName;
        command.Parameters.Add(columnNameParameter);

        var result = command.ExecuteScalar();
        return result is bool exists && exists;
    }
    finally
    {
        db.Database.CloseConnection();
    }
}

static bool DatabaseColumnIsNotNullable(AppDbContext db, string tableName, string columnName)
{
    db.Database.OpenConnection();

    try
    {
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = @"SELECT EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
              AND lower(table_name) = lower(@tableName)
              AND lower(column_name) = lower(@columnName)
              AND is_nullable = 'NO'
        )";

        var tableNameParameter = command.CreateParameter();
        tableNameParameter.ParameterName = "@tableName";
        tableNameParameter.Value = tableName;
        command.Parameters.Add(tableNameParameter);

        var columnNameParameter = command.CreateParameter();
        columnNameParameter.ParameterName = "@columnName";
        columnNameParameter.Value = columnName;
        command.Parameters.Add(columnNameParameter);

        var result = command.ExecuteScalar();
        return result is bool exists && exists;
    }
    finally
    {
        db.Database.CloseConnection();
    }
}

app.Run();
