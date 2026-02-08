using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatchNotes.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddTagPrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TagPrefix",
                table: "Packages",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TagPrefix",
                table: "Packages");
        }
    }
}
