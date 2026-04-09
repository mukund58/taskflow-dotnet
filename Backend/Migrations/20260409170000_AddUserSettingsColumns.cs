using Backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260409170000_AddUserSettingsColumns")]
    public partial class AddUserSettingsColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
DECLARE table_record RECORD;
BEGIN
    FOR table_record IN
        SELECT table_schema, table_name
        FROM information_schema.tables
        WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
          AND lower(table_name) = 'users'
    LOOP
        EXECUTE format(
            'ALTER TABLE %I.%I ADD COLUMN IF NOT EXISTS %I text NOT NULL DEFAULT %L',
            table_record.table_schema,
            table_record.table_name,
            'Theme',
            'system');

        EXECUTE format(
            'ALTER TABLE %I.%I ADD COLUMN IF NOT EXISTS %I text NOT NULL DEFAULT %L',
            table_record.table_schema,
            table_record.table_name,
            'Language',
            'en');

        EXECUTE format(
            'ALTER TABLE %I.%I ADD COLUMN IF NOT EXISTS %I text NOT NULL DEFAULT %L',
            table_record.table_schema,
            table_record.table_name,
            'Timezone',
            'UTC');

        EXECUTE format(
            'ALTER TABLE %I.%I ADD COLUMN IF NOT EXISTS %I boolean NOT NULL DEFAULT true',
            table_record.table_schema,
            table_record.table_name,
            'EmailNotificationsEnabled');

        EXECUTE format(
            'ALTER TABLE %I.%I ADD COLUMN IF NOT EXISTS %I boolean NOT NULL DEFAULT true',
            table_record.table_schema,
            table_record.table_name,
            'PushNotificationsEnabled');
    END LOOP;
END $$;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
DECLARE table_record RECORD;
BEGIN
    FOR table_record IN
        SELECT table_schema, table_name
        FROM information_schema.tables
        WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
          AND lower(table_name) = 'users'
    LOOP
        EXECUTE format(
            'ALTER TABLE %I.%I DROP COLUMN IF EXISTS %I',
            table_record.table_schema,
            table_record.table_name,
            'Theme');

        EXECUTE format(
            'ALTER TABLE %I.%I DROP COLUMN IF EXISTS %I',
            table_record.table_schema,
            table_record.table_name,
            'Language');

        EXECUTE format(
            'ALTER TABLE %I.%I DROP COLUMN IF EXISTS %I',
            table_record.table_schema,
            table_record.table_name,
            'Timezone');

        EXECUTE format(
            'ALTER TABLE %I.%I DROP COLUMN IF EXISTS %I',
            table_record.table_schema,
            table_record.table_name,
            'EmailNotificationsEnabled');

        EXECUTE format(
            'ALTER TABLE %I.%I DROP COLUMN IF EXISTS %I',
            table_record.table_schema,
            table_record.table_name,
            'PushNotificationsEnabled');
    END LOOP;
END $$;");
        }
    }
}
