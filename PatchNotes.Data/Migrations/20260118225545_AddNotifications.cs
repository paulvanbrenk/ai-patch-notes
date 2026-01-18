using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatchNotes.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GitHubId = table.Column<string>(type: "TEXT", nullable: false),
                    PackageId = table.Column<int>(type: "INTEGER", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectTitle = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectType = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectUrl = table.Column<string>(type: "TEXT", nullable: true),
                    RepositoryFullName = table.Column<string>(type: "TEXT", nullable: false),
                    Unread = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastReadAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FetchedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_GitHubId",
                table: "Notifications",
                column: "GitHubId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_PackageId",
                table: "Notifications",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Unread",
                table: "Notifications",
                column: "Unread");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UpdatedAt",
                table: "Notifications",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}
