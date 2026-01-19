using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatchNotes.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Releases_PackageId",
                table: "Releases");

            migrationBuilder.DropIndex(
                name: "IX_Packages_NpmName",
                table: "Packages");

            migrationBuilder.CreateIndex(
                name: "IX_Releases_PackageId_Tag",
                table: "Releases",
                columns: new[] { "PackageId", "Tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Packages_NpmName",
                table: "Packages",
                column: "NpmName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Releases_PackageId_Tag",
                table: "Releases");

            migrationBuilder.DropIndex(
                name: "IX_Packages_NpmName",
                table: "Packages");

            migrationBuilder.CreateIndex(
                name: "IX_Releases_PackageId",
                table: "Releases",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_NpmName",
                table: "Packages",
                column: "NpmName");
        }
    }
}
