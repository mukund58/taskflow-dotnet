using Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Backend.Extensions;

public static class DatabaseExtensions
{
    public static async Task ApplyMigrationsAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seedingOptions = scope.ServiceProvider.GetRequiredService<IOptions<SeedingOptions>>().Value;

        try
        {
            // First, let's just make sure the database and all migrations are applied.
            // GetPendingMigrationsAsync() will throw if the database does not exist at all,
            // but Migrate() will create the database properly before doing anything else.
            // Simple test to see if database exists
            bool dbExists = false;
            try
            {
                db.Database.OpenConnection();
                dbExists = true;
                db.Database.CloseConnection();
            }
            catch (PostgresException ex) when (ex.SqlState == "3D000") // invalid_catalog_name
            {
                // Database doesn't exist
            }

            var hasProjectTable = dbExists && DatabaseTableExists(db, "Projects");

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

                var nextMigrations = (await db.Database.GetPendingMigrationsAsync()).ToList();

                if (nextMigrations.Count > 0)
                {
                    try
                    {
                        db.Database.Migrate();
                        app.Logger.LogInformation(
                            "Applied {PendingMigrationCount} pending migrations",
                            nextMigrations.Count);
                    }
                    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.DuplicateTable)
                    {
                        app.Logger.LogError(
                            ex,
                            "Migration failed with duplicate table while applying: {PendingMigrations}",
                            string.Join(", ", nextMigrations));

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

    private static int AlignMigrationHistoryWithExistingSchema(AppDbContext db, ILogger logger)
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

    private static void EnsureMigrationHistoryTable(AppDbContext db)
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

    private static HashSet<string> GetAppliedMigrationIds(AppDbContext db)
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

    private static void InsertMigrationHistoryRow(AppDbContext db, string migrationId, string productVersion)
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

    private static int RepairCriticalSchemaDrift(AppDbContext db, ILogger logger)
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

    private static void EnsureDueDateColumnExistsForTasks(AppDbContext db)
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

    private static void EnsureDueDateColumnExistsForProjects(AppDbContext db)
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

    private static void EnsureLegacyCreatedByColumnIsNullableForTasks(AppDbContext db)
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

    private static bool DatabaseTableExists(AppDbContext db, string tableName)
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

    private static bool DatabaseColumnExists(AppDbContext db, string tableName, string columnName)
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

    private static bool DatabaseColumnIsNotNullable(AppDbContext db, string tableName, string columnName)
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
}
