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
            // SQL Server cannot ALTER COLUMN to remove IDENTITY, so we use raw SQL.
            // Dynamically drop all FKs, indexes, and PKs to avoid hardcoded name mismatches.
            // Early-stage app — data loss is acceptable.
            migrationBuilder.Sql(@"
                -- Dynamically drop ALL foreign keys referencing these tables
                DECLARE @sql NVARCHAR(MAX) = N'';
                SELECT @sql += 'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + '.' + QUOTENAME(OBJECT_NAME(parent_object_id))
                    + ' DROP CONSTRAINT ' + QUOTENAME(name) + ';' + CHAR(13)
                FROM sys.foreign_keys
                WHERE referenced_object_id IN (OBJECT_ID('Packages'), OBJECT_ID('Users'))
                   OR parent_object_id IN (OBJECT_ID('Watchlists'), OBJECT_ID('Releases'), OBJECT_ID('Notifications'));
                EXEC sp_executesql @sql;
            ");

            migrationBuilder.Sql(@"
                -- Dynamically drop ALL non-PK indexes on affected tables
                DECLARE @sql NVARCHAR(MAX) = N'';
                SELECT @sql += 'DROP INDEX ' + QUOTENAME(i.name) + ' ON ' + QUOTENAME(OBJECT_SCHEMA_NAME(i.object_id)) + '.' + QUOTENAME(OBJECT_NAME(i.object_id)) + ';' + CHAR(13)
                FROM sys.indexes i
                WHERE i.object_id IN (OBJECT_ID('Packages'), OBJECT_ID('Users'), OBJECT_ID('Releases'), OBJECT_ID('Notifications'), OBJECT_ID('Watchlists'))
                  AND i.is_primary_key = 0
                  AND i.type > 0;
                EXEC sp_executesql @sql;
            ");

            migrationBuilder.Sql(@"
                -- Clear all data
                DELETE FROM [Watchlists];
                DELETE FROM [Notifications];
                DELETE FROM [Releases];
                DELETE FROM [Users];
                DELETE FROM [Packages];
                DELETE FROM [ProcessedWebhookEvents];

                -- Recreate PK columns: drop PK, drop old int IDENTITY col, add new nvarchar col, add PK
                ALTER TABLE [Packages] DROP CONSTRAINT [PK_Packages];
                ALTER TABLE [Packages] DROP COLUMN [Id];
                ALTER TABLE [Packages] ADD [Id] nvarchar(21) NOT NULL DEFAULT '';
                ALTER TABLE [Packages] ADD CONSTRAINT [PK_Packages] PRIMARY KEY ([Id]);

                ALTER TABLE [Users] DROP CONSTRAINT [PK_Users];
                ALTER TABLE [Users] DROP COLUMN [Id];
                ALTER TABLE [Users] ADD [Id] nvarchar(21) NOT NULL DEFAULT '';
                ALTER TABLE [Users] ADD CONSTRAINT [PK_Users] PRIMARY KEY ([Id]);

                ALTER TABLE [Releases] DROP CONSTRAINT [PK_Releases];
                ALTER TABLE [Releases] DROP COLUMN [Id];
                ALTER TABLE [Releases] ADD [Id] nvarchar(21) NOT NULL DEFAULT '';
                ALTER TABLE [Releases] ADD CONSTRAINT [PK_Releases] PRIMARY KEY ([Id]);

                ALTER TABLE [Notifications] DROP CONSTRAINT [PK_Notifications];
                ALTER TABLE [Notifications] DROP COLUMN [Id];
                ALTER TABLE [Notifications] ADD [Id] nvarchar(21) NOT NULL DEFAULT '';
                ALTER TABLE [Notifications] ADD CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]);

                ALTER TABLE [Watchlists] DROP CONSTRAINT [PK_Watchlists];
                ALTER TABLE [Watchlists] DROP COLUMN [Id];
                ALTER TABLE [Watchlists] ADD [Id] nvarchar(21) NOT NULL DEFAULT '';
                ALTER TABLE [Watchlists] ADD CONSTRAINT [PK_Watchlists] PRIMARY KEY ([Id]);

                -- Alter FK columns from int to nvarchar(21)
                ALTER TABLE [Releases] ALTER COLUMN [PackageId] nvarchar(21) NOT NULL;
                ALTER TABLE [Notifications] ALTER COLUMN [PackageId] nvarchar(21) NULL;
                ALTER TABLE [Watchlists] ALTER COLUMN [UserId] nvarchar(21) NOT NULL;
                ALTER TABLE [Watchlists] ALTER COLUMN [PackageId] nvarchar(21) NOT NULL;

                -- Recreate indexes
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
