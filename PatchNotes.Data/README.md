# PatchNotes.Data

Data layer for the PatchNotes application, providing Entity Framework Core models, database context, and external API clients.

## Overview

This project contains:

- **Entity Models** - `Package`, `Release`, `Notification`, `User`
- **Database Context** - `PatchNotesDbContext` with SQLite/SQL Server support
- **EF Core Migrations** - Database schema versioning
- **GitHub Client** - Fetches releases from GitHub repositories
- **AI Client** - Generates release note summaries via LLM API (Groq-compatible)
- **Stytch Client** - User authentication via Stytch

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
// Database context
builder.Services.AddPatchNotesDbContext(configuration);

// GitHub client
builder.Services.AddGitHubClient(options => {
    options.Token = configuration["GitHub:Token"];
});

// AI client
builder.Services.AddAiClient(options => {
    options.ApiKey = configuration["AI:ApiKey"];
});

// Stytch client
builder.Services.AddStytchClient(options => {
    options.ProjectId = configuration["Stytch:ProjectId"];
    options.Secret = configuration["Stytch:Secret"];
});
```

## Directory Structure

```
PatchNotes.Data/
├── Migrations/           # EF Core migrations
├── GitHub/               # GitHub API client
├── AI/                   # AI/LLM client for summaries
├── Stytch/               # Stytch authentication client
├── Package.cs            # Package entity
├── Release.cs            # Release entity
├── Notification.cs       # Notification entity
├── User.cs               # User entity
├── PatchNotesDbContext.cs
├── DbSeeder.cs           # Database seeding
└── DatabaseProviderFactory.cs
```
