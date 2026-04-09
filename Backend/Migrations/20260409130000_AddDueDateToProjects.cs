using Backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260409130000_AddDueDateToProjects")]
    public partial class AddDueDateToProjects : Migration
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
          AND lower(table_name) = 'projects'
    LOOP
        EXECUTE format(
            'ALTER TABLE %I.%I ADD COLUMN IF NOT EXISTS %I timestamp with time zone',
            table_record.table_schema,
            table_record.table_name,
            'DueDate');
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
          AND lower(table_name) = 'projects'
    LOOP
        EXECUTE format(
            'ALTER TABLE %I.%I DROP COLUMN IF EXISTS %I',
            table_record.table_schema,
            table_record.table_name,
            'DueDate');
    END LOOP;
END $$;");
        }
    }
}
