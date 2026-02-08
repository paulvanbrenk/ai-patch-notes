using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatchNotes.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class DenormalizeVersionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrerelease",
                table: "Releases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MajorVersion",
                table: "Releases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinorVersion",
                table: "Releases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PatchVersion",
                table: "Releases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Releases_PackageId_MajorVersion_IsPrerelease",
                table: "Releases",
                columns: new[] { "PackageId", "MajorVersion", "IsPrerelease" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Releases_PackageId_MajorVersion_IsPrerelease",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "IsPrerelease",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "MajorVersion",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "MinorVersion",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "PatchVersion",
                table: "Releases");
        }
    }
}
