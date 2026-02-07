using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatchNotes.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class MigrateToNanoidIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop all foreign key constraints
            migrationBuilder.DropForeignKey(name: "FK_Watchlists_Users_UserId", table: "Watchlists");
            migrationBuilder.DropForeignKey(name: "FK_Watchlists_Packages_PackageId", table: "Watchlists");
            migrationBuilder.DropForeignKey(name: "FK_Releases_Packages_PackageId", table: "Releases");
            migrationBuilder.DropForeignKey(name: "FK_Notifications_Packages_PackageId", table: "Notifications");

            // Drop indexes on FK columns
            migrationBuilder.DropIndex(name: "IX_Watchlists_PackageId", table: "Watchlists");
            migrationBuilder.DropIndex(name: "IX_Watchlists_UserId", table: "Watchlists");
            migrationBuilder.DropIndex(name: "IX_Releases_PackageId", table: "Releases");
            migrationBuilder.DropIndex(name: "IX_Notifications_PackageId", table: "Notifications");

            // Truncate all tables (early-stage app, no production data to preserve)
            migrationBuilder.Sql("DELETE FROM [Watchlists];");
            migrationBuilder.Sql("DELETE FROM [Notifications];");
            migrationBuilder.Sql("DELETE FROM [Releases];");
            migrationBuilder.Sql("DELETE FROM [Users];");
            migrationBuilder.Sql("DELETE FROM [Packages];");
            migrationBuilder.Sql("DELETE FROM [ProcessedWebhookEvents];");

            // Alter PK columns from int identity to nvarchar(21)
            migrationBuilder.AlterColumn<string>(
                name: "Id", table: "Packages",
                type: "nvarchar(21)", maxLength: 21, nullable: false,
                oldClrType: typeof(int), oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "Id", table: "Users",
                type: "nvarchar(21)", maxLength: 21, nullable: false,
                oldClrType: typeof(int), oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "Id", table: "Releases",
                type: "nvarchar(21)", maxLength: 21, nullable: false,
                oldClrType: typeof(int), oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "Id", table: "Notifications",
                type: "nvarchar(21)", maxLength: 21, nullable: false,
                oldClrType: typeof(int), oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "Id", table: "Watchlists",
                type: "nvarchar(21)", maxLength: 21, nullable: false,
                oldClrType: typeof(int), oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            // Alter FK columns from int to nvarchar(21)
            migrationBuilder.AlterColumn<string>(
                name: "PackageId", table: "Releases",
                type: "nvarchar(21)", maxLength: 21, nullable: false,
                oldClrType: typeof(int), oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "PackageId", table: "Notifications",
                type: "nvarchar(21)", maxLength: 21, nullable: true,
                oldClrType: typeof(int), oldType: "int", oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId", table: "Watchlists",
                type: "nvarchar(21)", maxLength: 21, nullable: false,
                oldClrType: typeof(int), oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "PackageId", table: "Watchlists",
                type: "nvarchar(21)", maxLength: 21, nullable: false,
                oldClrType: typeof(int), oldType: "int");

            // Recreate indexes on FK columns
            migrationBuilder.CreateIndex(name: "IX_Releases_PackageId", table: "Releases", column: "PackageId");
            migrationBuilder.CreateIndex(name: "IX_Notifications_PackageId", table: "Notifications", column: "PackageId");
            migrationBuilder.CreateIndex(name: "IX_Watchlists_PackageId", table: "Watchlists", column: "PackageId");
            migrationBuilder.CreateIndex(name: "IX_Watchlists_UserId", table: "Watchlists", column: "UserId");

            // Recreate foreign key constraints
            migrationBuilder.AddForeignKey(
                name: "FK_Releases_Packages_PackageId", table: "Releases",
                column: "PackageId", principalTable: "Packages", principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Packages_PackageId", table: "Notifications",
                column: "PackageId", principalTable: "Packages", principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Watchlists_Packages_PackageId", table: "Watchlists",
                column: "PackageId", principalTable: "Packages", principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Watchlists_Users_UserId", table: "Watchlists",
                column: "UserId", principalTable: "Users", principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Watchlists",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(21)",
                oldMaxLength: 21);

            migrationBuilder.AlterColumn<int>(
                name: "PackageId",
                table: "Watchlists",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(21)",
                oldMaxLength: 21);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Watchlists",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(21)",
                oldMaxLength: 21)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(21)",
                oldMaxLength: 21)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "PackageId",
                table: "Releases",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(21)",
                oldMaxLength: 21);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Releases",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(21)",
                oldMaxLength: 21)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Packages",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(21)",
                oldMaxLength: 21)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "PackageId",
                table: "Notifications",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(21)",
                oldMaxLength: 21,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Notifications",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(21)",
                oldMaxLength: 21)
                .Annotation("SqlServer:Identity", "1, 1");
        }
    }
}
