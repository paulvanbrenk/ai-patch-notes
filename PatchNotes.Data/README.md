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

The application supports both SQLite (development) and SQL Server (production).

### Running Migrations

```bash
cd PatchNotes.Data
dotnet ef database update
```

### Creating a New Migration

```bash
dotnet ef migrations add MigrationName
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
