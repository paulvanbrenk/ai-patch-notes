using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatchNotes.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddReleaseSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReleaseSummaries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false),
                    PackageId = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false),
                    MajorVersion = table.Column<int>(type: "int", nullable: false),
                    IsPrerelease = table.Column<bool>(type: "bit", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseSummaries_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseSummaries_PackageId_MajorVersion_IsPrerelease",
                table: "ReleaseSummaries",
                columns: new[] { "PackageId", "MajorVersion", "IsPrerelease" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReleaseSummaries");
        }
    }
}
