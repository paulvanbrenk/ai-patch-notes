# PatchNotes.Data

Data layer for the PatchNotes application, providing Entity Framework Core models, database context, and shared utilities.

## Overview

This project contains:

- **Entity Models** - `Package`, `Release`, `ReleaseSummary`, `User`, `Watchlist`, `ProcessedWebhookEvent`
- **Database Context** - `PatchNotesDbContext` with SQLite/SQL Server support
- **EF Core Migrations** - Database schema versioning (Sqlite + SqlServer)
- **Version Parsing** - `VersionParser` for semver extraction from release tags
- **Summary Constants** - Shared constants for AI summary generation
- **Database Seeding** - `DbSeeder` and default watchlist configuration
- **ID Generation** - Nanoid-based 21-character unique IDs

## Database

The application supports both SQLite (development) and SQL Server (production) with separate migration folders.

```
Migrations/
├── Sqlite/      # Local development migrations
└── SqlServer/   # Production migrations
```

### Creating a New Migration

Use the helper script to generate migrations for both providers:

```bash
# Set SQL Server connection string (required for SqlServer migrations)
export ConnectionStrings__PatchNotes="Server=...;Database=...;User Id=...;Password=..."

# Generate both migrations
./scripts/add-migration.sh MigrationName
```

Or manually:

```bash
# SQLite migration
dotnet ef migrations add MigrationName --context SqliteContext --output-dir Migrations/Sqlite --startup-project ../PatchNotes.Api

# SQL Server migration (requires connection string env var)
dotnet ef migrations add MigrationName --context SqlServerContext --output-dir Migrations/SqlServer --startup-project ../PatchNotes.Api
```

### Running Migrations

```bash
# Local (SQLite)
dotnet ef database update --context SqliteContext --startup-project ../PatchNotes.Api

# Production (SQL Server) - done automatically by GitHub Actions
dotnet ef database update --context SqlServerContext --startup-project ../PatchNotes.Api
```

## Service Registration

Register services in your host:

```csharp
// Database context (auto-detects SQLite vs SQL Server from config)
builder.Services.AddPatchNotesDbContext(configuration);
```

## Directory Structure

```
PatchNotes.Data/
├── Migrations/
│   ├── Sqlite/               # SQLite migrations (4 migrations)
│   └── SqlServer/            # SQL Server migrations (15 migrations)
├── SeedData/
│   ├── packages.json         # Package catalog
│   └── SeedDataModels.cs     # Seed data types
├── Package.cs                # Package entity
├── Release.cs                # Release entity
├── ReleaseSummary.cs         # AI summary entity
├── User.cs                   # User entity (Stytch + Stripe + email prefs)
├── Watchlist.cs              # User watchlist entity
├── ProcessedWebhookEvent.cs  # Webhook deduplication
├── PatchNotesDbContext.cs    # DbContext (SqliteContext, SqlServerContext)
├── DbSeeder.cs               # Database seeding
├── DefaultWatchlistOptions.cs # Default watchlist configuration
├── IdGenerator.cs            # Nanoid ID generation (21-char)
├── VersionParser.cs          # Semver parsing from tags
├── SummaryConstants.cs       # AI summary constants
└── IHasTimestamps.cs         # Timestamp interface
```
