using Backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260410000000_AddMissingTables")]
    public partial class AddMissingTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── RefreshTokens ──────────────────────────────────────────────
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""RefreshTokens"" (
    ""Id""                 uuid NOT NULL DEFAULT gen_random_uuid(),
    ""Token""              text NOT NULL,
    ""UserId""             uuid NOT NULL,
    ""ExpiresAt""          timestamp with time zone NOT NULL,
    ""CreatedAt""          timestamp with time zone NOT NULL,
    ""RevokedAt""          timestamp with time zone,
    ""ReplacedByTokenId""  uuid,
    CONSTRAINT ""PK_RefreshTokens"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_RefreshTokens_Users_UserId"" FOREIGN KEY (""UserId"")
        REFERENCES ""Users"" (""Id"") ON DELETE CASCADE,
    CONSTRAINT ""FK_RefreshTokens_RefreshTokens_ReplacedByTokenId"" FOREIGN KEY (""ReplacedByTokenId"")
        REFERENCES ""RefreshTokens"" (""Id"") ON DELETE SET NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_RefreshTokens_Token""
    ON ""RefreshTokens"" (""Token"");

CREATE INDEX IF NOT EXISTS ""IX_RefreshTokens_UserId""
    ON ""RefreshTokens"" (""UserId"");

CREATE INDEX IF NOT EXISTS ""IX_RefreshTokens_ReplacedByTokenId""
    ON ""RefreshTokens"" (""ReplacedByTokenId"");
");

            // ── Labels ─────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""Labels"" (
    ""Id""          uuid NOT NULL DEFAULT gen_random_uuid(),
    ""Name""        text NOT NULL,
    ""Color""       text NOT NULL DEFAULT '#808080',
    ""Description"" text NOT NULL DEFAULT '',
    ""ProjectId""   uuid NOT NULL,
    ""CreatedAt""   timestamp with time zone NOT NULL,
    ""IsDeleted""   boolean NOT NULL DEFAULT false,
    CONSTRAINT ""PK_Labels"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_Labels_Projects_ProjectId"" FOREIGN KEY (""ProjectId"")
        REFERENCES ""Projects"" (""Id"") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ""IX_Labels_ProjectId""
    ON ""Labels"" (""ProjectId"");
");

            // ── TaskLabels (join table) ────────────────────────────────────
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""TaskLabels"" (
    ""TaskId""   uuid NOT NULL,
    ""LabelId""  uuid NOT NULL,
    CONSTRAINT ""PK_TaskLabels"" PRIMARY KEY (""TaskId"", ""LabelId""),
    CONSTRAINT ""FK_TaskLabels_Tasks_TaskId"" FOREIGN KEY (""TaskId"")
        REFERENCES ""Tasks"" (""Id"") ON DELETE CASCADE,
    CONSTRAINT ""FK_TaskLabels_Labels_LabelId"" FOREIGN KEY (""LabelId"")
        REFERENCES ""Labels"" (""Id"") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ""IX_TaskLabels_LabelId""
    ON ""TaskLabels"" (""LabelId"");
");

            // ── TaskAttachments ────────────────────────────────────────────
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""TaskAttachments"" (
    ""Id""               uuid NOT NULL DEFAULT gen_random_uuid(),
    ""FileName""         text NOT NULL,
    ""FileSizeBytes""    bigint NOT NULL DEFAULT 0,
    ""FileExtension""   text NOT NULL DEFAULT '',
    ""StoragePath""     text NOT NULL DEFAULT '',
    ""TaskId""           uuid NOT NULL,
    ""UploadedByUserId"" uuid NOT NULL,
    ""UploadedAt""       timestamp with time zone NOT NULL,
    ""IsDeleted""        boolean NOT NULL DEFAULT false,
    CONSTRAINT ""PK_TaskAttachments"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_TaskAttachments_Tasks_TaskId"" FOREIGN KEY (""TaskId"")
        REFERENCES ""Tasks"" (""Id"") ON DELETE CASCADE,
    CONSTRAINT ""FK_TaskAttachments_Users_UploadedByUserId"" FOREIGN KEY (""UploadedByUserId"")
        REFERENCES ""Users"" (""Id"") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ""IX_TaskAttachments_TaskId""
    ON ""TaskAttachments"" (""TaskId"");

CREATE INDEX IF NOT EXISTS ""IX_TaskAttachments_UploadedByUserId""
    ON ""TaskAttachments"" (""UploadedByUserId"");
");

            // ── TaskWatchers (join table) ──────────────────────────────────
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""TaskWatchers"" (
    ""TaskId""       uuid NOT NULL,
    ""UserId""       uuid NOT NULL,
    ""WatchedSince"" timestamp with time zone NOT NULL,
    CONSTRAINT ""PK_TaskWatchers"" PRIMARY KEY (""TaskId"", ""UserId""),
    CONSTRAINT ""FK_TaskWatchers_Tasks_TaskId"" FOREIGN KEY (""TaskId"")
        REFERENCES ""Tasks"" (""Id"") ON DELETE CASCADE,
    CONSTRAINT ""FK_TaskWatchers_Users_UserId"" FOREIGN KEY (""UserId"")
        REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ""IX_TaskWatchers_UserId""
    ON ""TaskWatchers"" (""UserId"");
");

            // ── Notifications ──────────────────────────────────────────────
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""Notifications"" (
    ""Id""         uuid NOT NULL DEFAULT gen_random_uuid(),
    ""Message""    text NOT NULL,
    ""Type""       text NOT NULL DEFAULT '',
    ""UserId""     uuid NOT NULL,
    ""TaskId""     uuid,
    ""CommentId""  uuid,
    ""IsRead""     boolean NOT NULL DEFAULT false,
    ""CreatedAt""  timestamp with time zone NOT NULL,
    ""ReadAt""     timestamp with time zone,
    ""IsDeleted""  boolean NOT NULL DEFAULT false,
    CONSTRAINT ""PK_Notifications"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_Notifications_Users_UserId"" FOREIGN KEY (""UserId"")
        REFERENCES ""Users"" (""Id"") ON DELETE CASCADE,
    CONSTRAINT ""FK_Notifications_Tasks_TaskId"" FOREIGN KEY (""TaskId"")
        REFERENCES ""Tasks"" (""Id"") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS ""IX_Notifications_UserId""
    ON ""Notifications"" (""UserId"");

CREATE INDEX IF NOT EXISTS ""IX_Notifications_TaskId""
    ON ""Notifications"" (""TaskId"");
");

            // ── ProjectMembers (composite PK) ─────────────────────────────
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""ProjectMembers"" (
    ""ProjectId""     uuid NOT NULL,
    ""UserId""        uuid NOT NULL,
    ""Role""          text NOT NULL DEFAULT 'Member',
    ""AddedByUserId"" uuid NOT NULL,
    ""AddedAt""       timestamp with time zone NOT NULL,
    CONSTRAINT ""PK_ProjectMembers"" PRIMARY KEY (""ProjectId"", ""UserId""),
    CONSTRAINT ""FK_ProjectMembers_Projects_ProjectId"" FOREIGN KEY (""ProjectId"")
        REFERENCES ""Projects"" (""Id"") ON DELETE CASCADE,
    CONSTRAINT ""FK_ProjectMembers_Users_UserId"" FOREIGN KEY (""UserId"")
        REFERENCES ""Users"" (""Id"") ON DELETE CASCADE,
    CONSTRAINT ""FK_ProjectMembers_Users_AddedByUserId"" FOREIGN KEY (""AddedByUserId"")
        REFERENCES ""Users"" (""Id"") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ""IX_ProjectMembers_UserId""
    ON ""ProjectMembers"" (""UserId"");

CREATE INDEX IF NOT EXISTS ""IX_ProjectMembers_AddedByUserId""
    ON ""ProjectMembers"" (""AddedByUserId"");

CREATE INDEX IF NOT EXISTS ""IX_ProjectMembers_ProjectId_Role""
    ON ""ProjectMembers"" (""ProjectId"", ""Role"");
");

            // ── ProjectInvitations ─────────────────────────────────────────
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""ProjectInvitations"" (
    ""Id""              uuid NOT NULL DEFAULT gen_random_uuid(),
    ""ProjectId""       uuid NOT NULL,
    ""Email""           text NOT NULL,
    ""Role""            text NOT NULL DEFAULT 'Member',
    ""Status""          text NOT NULL DEFAULT 'Pending',
    ""InvitedByUserId"" uuid NOT NULL,
    ""CreatedAt""       timestamp with time zone NOT NULL,
    ""ExpiresAt""       timestamp with time zone,
    CONSTRAINT ""PK_ProjectInvitations"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_ProjectInvitations_Projects_ProjectId"" FOREIGN KEY (""ProjectId"")
        REFERENCES ""Projects"" (""Id"") ON DELETE CASCADE,
    CONSTRAINT ""FK_ProjectInvitations_Users_InvitedByUserId"" FOREIGN KEY (""InvitedByUserId"")
        REFERENCES ""Users"" (""Id"") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ""IX_ProjectInvitations_ProjectId_Email_Status""
    ON ""ProjectInvitations"" (""ProjectId"", ""Email"", ""Status"");

CREATE INDEX IF NOT EXISTS ""IX_ProjectInvitations_InvitedByUserId""
    ON ""ProjectInvitations"" (""InvitedByUserId"");
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS ""ProjectInvitations"";
DROP TABLE IF EXISTS ""ProjectMembers"";
DROP TABLE IF EXISTS ""Notifications"";
DROP TABLE IF EXISTS ""TaskWatchers"";
DROP TABLE IF EXISTS ""TaskAttachments"";
DROP TABLE IF EXISTS ""TaskLabels"";
DROP TABLE IF EXISTS ""Labels"";
DROP TABLE IF EXISTS ""RefreshTokens"";
");
        }
    }
}
