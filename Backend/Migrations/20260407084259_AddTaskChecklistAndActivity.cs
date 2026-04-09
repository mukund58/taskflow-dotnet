using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskChecklistAndActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'ChecklistItems'
    ) THEN
        CREATE TABLE ""ChecklistItems"" (
            ""Id"" uuid NOT NULL,
            ""TaskItemId"" uuid NOT NULL,
            ""Title"" text NOT NULL,
            ""IsCompleted"" boolean NOT NULL,
            ""Position"" integer NOT NULL,
            ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
            ""CreatedAt"" timestamp with time zone NOT NULL,
            ""CompletedAt"" timestamp with time zone,
            CONSTRAINT ""PK_ChecklistItems"" PRIMARY KEY (""Id""),
            CONSTRAINT ""FK_ChecklistItems_Tasks_TaskItemId"" FOREIGN KEY (""TaskItemId"") REFERENCES ""Tasks"" (""Id"") ON DELETE CASCADE
        );
    ELSE
        IF EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'ChecklistItems'
              AND column_name = 'TaskId'
        ) AND NOT EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'ChecklistItems'
              AND column_name = 'TaskItemId'
        ) THEN
            ALTER TABLE ""ChecklistItems"" RENAME COLUMN ""TaskId"" TO ""TaskItemId"";
        END IF;

        IF EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'ChecklistItems'
              AND column_name = 'Order'
        ) AND NOT EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'ChecklistItems'
              AND column_name = 'Position'
        ) THEN
            ALTER TABLE ""ChecklistItems"" RENAME COLUMN ""Order"" TO ""Position"";
        END IF;

        IF NOT EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'ChecklistItems'
              AND column_name = 'IsDeleted'
        ) THEN
            ALTER TABLE ""ChecklistItems"" ADD COLUMN ""IsDeleted"" boolean NOT NULL DEFAULT FALSE;
        END IF;
    END IF;
END $$;");

            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ""IX_ChecklistItems_TaskId"";
CREATE INDEX IF NOT EXISTS ""IX_ChecklistItems_TaskItemId_Position"" ON ""ChecklistItems"" (""TaskItemId"", ""Position"");");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""TaskActivities"" (
    ""Id"" uuid NOT NULL,
    ""TaskItemId"" uuid NOT NULL,
    ""Action"" text NOT NULL,
    ""OldValue"" text,
    ""NewValue"" text,
    ""ActorUserId"" uuid NOT NULL,
    ""CreatedAt"" timestamp with time zone NOT NULL,
    CONSTRAINT ""PK_TaskActivities"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_TaskActivities_Tasks_TaskItemId"" FOREIGN KEY (""TaskItemId"") REFERENCES ""Tasks"" (""Id"") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ""IX_TaskActivities_TaskItemId_CreatedAt"" ON ""TaskActivities"" (""TaskItemId"", ""CreatedAt"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChecklistItems");

            migrationBuilder.DropTable(
                name: "TaskActivities");
        }
    }
}
