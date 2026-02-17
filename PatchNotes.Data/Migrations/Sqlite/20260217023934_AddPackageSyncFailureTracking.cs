using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatchNotes.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddPackageSyncFailureTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveFailures",
                table: "Packages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsSyncDisabled",
                table: "Packages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFailureAt",
                table: "Packages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastFailureMessage",
                table: "Packages",
                type: "TEXT",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsecutiveFailures",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "IsSyncDisabled",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "LastFailureAt",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "LastFailureMessage",
                table: "Packages");
        }
    }
}
