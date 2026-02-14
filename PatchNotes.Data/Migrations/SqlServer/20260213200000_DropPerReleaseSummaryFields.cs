using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatchNotes.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class DropPerReleaseSummaryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Summary",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "SummaryGeneratedAt",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "SummaryVersion",
                table: "Releases");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Releases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SummaryGeneratedAt",
                table: "Releases",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SummaryVersion",
                table: "Releases",
                type: "nvarchar(21)",
                maxLength: 21,
                nullable: true);
        }
    }
}
