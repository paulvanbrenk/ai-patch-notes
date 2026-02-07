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
            // SQL Server cannot ALTER COLUMN to remove IDENTITY, so we must use raw SQL
            // to drop and recreate the columns. Early-stage app — data loss is acceptable.
            migrationBuilder.Sql(@"
                -- Drop all FK constraints
                ALTER TABLE [Watchlists] DROP CONSTRAINT [FK_Watchlists_Users_UserId];
                ALTER TABLE [Watchlists] DROP CONSTRAINT [FK_Watchlists_Packages_PackageId];
                ALTER TABLE [Releases] DROP CONSTRAINT [FK_Releases_Packages_PackageId];
                ALTER TABLE [Notifications] DROP CONSTRAINT [FK_Notifications_Packages_PackageId];

                -- Drop FK indexes
                DROP INDEX [IX_Watchlists_PackageId] ON [Watchlists];
                DROP INDEX [IX_Watchlists_UserId] ON [Watchlists];
                DROP INDEX [IX_Releases_PackageId] ON [Releases];
                DROP INDEX [IX_Notifications_PackageId] ON [Notifications];

                -- Clear all data (order matters for FKs, but we already dropped them)
                DELETE FROM [Watchlists];
                DELETE FROM [Notifications];
                DELETE FROM [Releases];
                DELETE FROM [Users];
                DELETE FROM [Packages];
                DELETE FROM [ProcessedWebhookEvents];

                -- Recreate PK columns: drop PK, drop old int IDENTITY col, add new nvarchar col, add PK
                -- Packages
                ALTER TABLE [Packages] DROP CONSTRAINT [PK_Packages];
                ALTER TABLE [Packages] DROP COLUMN [Id];
                ALTER TABLE [Packages] ADD [Id] nvarchar(21) NOT NULL DEFAULT '';
                ALTER TABLE [Packages] ADD CONSTRAINT [PK_Packages] PRIMARY KEY ([Id]);

                -- Users
                ALTER TABLE [Users] DROP CONSTRAINT [PK_Users];
                ALTER TABLE [Users] DROP COLUMN [Id];
                ALTER TABLE [Users] ADD [Id] nvarchar(21) NOT NULL DEFAULT '';
                ALTER TABLE [Users] ADD CONSTRAINT [PK_Users] PRIMARY KEY ([Id]);

                -- Releases
                ALTER TABLE [Releases] DROP CONSTRAINT [PK_Releases];
                ALTER TABLE [Releases] DROP COLUMN [Id];
                ALTER TABLE [Releases] ADD [Id] nvarchar(21) NOT NULL DEFAULT '';
                ALTER TABLE [Releases] ADD CONSTRAINT [PK_Releases] PRIMARY KEY ([Id]);

                -- Notifications
                ALTER TABLE [Notifications] DROP CONSTRAINT [PK_Notifications];
                ALTER TABLE [Notifications] DROP COLUMN [Id];
                ALTER TABLE [Notifications] ADD [Id] nvarchar(21) NOT NULL DEFAULT '';
                ALTER TABLE [Notifications] ADD CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]);

                -- Watchlists
                ALTER TABLE [Watchlists] DROP CONSTRAINT [PK_Watchlists];
                ALTER TABLE [Watchlists] DROP COLUMN [Id];
                ALTER TABLE [Watchlists] ADD [Id] nvarchar(21) NOT NULL DEFAULT '';
                ALTER TABLE [Watchlists] ADD CONSTRAINT [PK_Watchlists] PRIMARY KEY ([Id]);

                -- Alter FK columns from int to nvarchar(21)
                ALTER TABLE [Releases] ALTER COLUMN [PackageId] nvarchar(21) NOT NULL;
                ALTER TABLE [Notifications] ALTER COLUMN [PackageId] nvarchar(21) NULL;
                ALTER TABLE [Watchlists] ALTER COLUMN [UserId] nvarchar(21) NOT NULL;
                ALTER TABLE [Watchlists] ALTER COLUMN [PackageId] nvarchar(21) NOT NULL;

                -- Recreate FK indexes
                CREATE INDEX [IX_Releases_PackageId] ON [Releases] ([PackageId]);
                CREATE INDEX [IX_Notifications_PackageId] ON [Notifications] ([PackageId]);
                CREATE INDEX [IX_Watchlists_PackageId] ON [Watchlists] ([PackageId]);
                CREATE INDEX [IX_Watchlists_UserId] ON [Watchlists] ([UserId]);

                -- Recreate FK constraints
                ALTER TABLE [Releases] ADD CONSTRAINT [FK_Releases_Packages_PackageId]
                    FOREIGN KEY ([PackageId]) REFERENCES [Packages] ([Id]) ON DELETE CASCADE;
                ALTER TABLE [Notifications] ADD CONSTRAINT [FK_Notifications_Packages_PackageId]
                    FOREIGN KEY ([PackageId]) REFERENCES [Packages] ([Id]);
                ALTER TABLE [Watchlists] ADD CONSTRAINT [FK_Watchlists_Packages_PackageId]
                    FOREIGN KEY ([PackageId]) REFERENCES [Packages] ([Id]) ON DELETE CASCADE;
                ALTER TABLE [Watchlists] ADD CONSTRAINT [FK_Watchlists_Users_UserId]
                    FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not implementing reverse migration — this is a one-way destructive change
            throw new NotSupportedException("Reverting nanoid migration is not supported.");
        }
    }
}
