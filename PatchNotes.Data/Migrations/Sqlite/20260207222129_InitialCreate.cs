using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatchNotes.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Packages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    NpmName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    GithubOwner = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    GithubRepo = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    LastFetchedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedWebhookEvents",
                columns: table => new
                {
                    EventId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedWebhookEvents", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    StytchUserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StripeCustomerId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    SubscriptionStatus = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    SubscriptionExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    GitHubId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PackageId = table.Column<string>(type: "TEXT", maxLength: 21, nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SubjectTitle = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SubjectUrl = table.Column<string>(type: "TEXT", nullable: true),
                    RepositoryFullName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
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

            migrationBuilder.CreateTable(
                name: "Releases",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    PackageId = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    Tag = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Body = table.Column<string>(type: "TEXT", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    SummaryGeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Releases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Releases_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseSummaries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    PackageId = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    MajorVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPrerelease = table.Column<bool>(type: "INTEGER", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "Watchlists",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    PackageId = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Watchlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Watchlists_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Watchlists_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateIndex(
                name: "IX_Packages_NpmName",
                table: "Packages",
                column: "NpmName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Releases_PackageId_Tag",
                table: "Releases",
                columns: new[] { "PackageId", "Tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Releases_PublishedAt",
                table: "Releases",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseSummaries_PackageId_MajorVersion_IsPrerelease",
                table: "ReleaseSummaries",
                columns: new[] { "PackageId", "MajorVersion", "IsPrerelease" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Users_StripeCustomerId",
                table: "Users",
                column: "StripeCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_StytchUserId",
                table: "Users",
                column: "StytchUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Watchlists_PackageId",
                table: "Watchlists",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_Watchlists_UserId_PackageId",
                table: "Watchlists",
                columns: new[] { "UserId", "PackageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "ProcessedWebhookEvents");

            migrationBuilder.DropTable(
                name: "Releases");

            migrationBuilder.DropTable(
                name: "ReleaseSummaries");

            migrationBuilder.DropTable(
                name: "Watchlists");

            migrationBuilder.DropTable(
                name: "Packages");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
