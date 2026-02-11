using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatchNotes.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class RepairReleaseSummaryColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Repair: Summary and SummaryGeneratedAt were in InitialCreate but may be
            // missing if the production database was created before those columns were
            // added to the migration. Use idempotent SQL to add them only if missing.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('Releases') AND name = 'Summary'
                )
                BEGIN
                    ALTER TABLE [Releases] ADD [Summary] nvarchar(max) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('Releases') AND name = 'SummaryGeneratedAt'
                )
                BEGIN
                    ALTER TABLE [Releases] ADD [SummaryGeneratedAt] datetime2 NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: these columns should have existed from InitialCreate
        }
    }
}
