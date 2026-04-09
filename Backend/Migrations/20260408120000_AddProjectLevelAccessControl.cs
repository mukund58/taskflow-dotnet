using System;
using Backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260408120000_AddProjectLevelAccessControl")]
    public partial class AddProjectLevelAccessControl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "Projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerUserId",
                table: "Projects",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_OwnerUserId",
                table: "Projects",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_OwnerUserId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_OwnerUserId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Projects");
        }
    }
}