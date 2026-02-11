# PatchNotes

A GitHub release viewer for npm packages. Track release notes across your favorite packages in one place.

*Forged in Gas Town*

## Deployment

| Environment | URL | Status |
|-------------|-----|--------|
| Frontend | https://app.mypkgupdate.com | Live |
| API | https://api-mypkgupdate-com.azurewebsites.net | Live |
| Sync Function | fn-patchnotes-sync (Azure Functions) | Timer (every 6h) |

## Project Status

**Stage:** Development (MVP)
**Health Score:** 8/10

| Area | Status |
|------|--------|
| Architecture | ✅ Solid (.NET + React separation) |
| Code Quality | ✅ Good |
| CI/CD | ✅ GitHub Actions (build, test, deploy) |
| Testing | ✅ 141 Vitest tests + xUnit API/Sync tests |
| Authentication | ✅ Stytch B2C configured |
| Error Handling | ✅ Error boundaries + toast notifications |

## Features

- **Package Tracking** - Add npm packages to monitor their GitHub releases
- **Release Timeline** - Mobile-first timeline view grouped by date
- **Package Picker** - Filter releases by selected packages
- **Sync Engine** - Fetch releases from GitHub with rate limit awareness
- **AI Summaries** - Generate concise release note summaries using Groq LLM
- **Design System** - Consistent visual language across components

## Architecture

- **PatchNotes.Data** - EF Core models, SQLite/SQL Server, GitHub API client, AI client
- **PatchNotes.Api** - ASP.NET Core Web API (port 5031)
- **PatchNotes.Sync** - CLI tool + SyncPipeline for concurrent sync & summary generation
- **PatchNotes.Functions** - Azure Functions timer trigger that runs the SyncPipeline every 6 hours
- **patchnotes-web** - React frontend with TanStack Router & Query

## Prerequisites

- .NET 10 SDK
- Node.js 18+
- [direnv](https://direnv.net/) (recommended for secrets management)

## Configuration

The project uses direnv for environment-based configuration. Create a secrets file at `~/.secrets/patchnotes/.env.local`:

```bash
# Required for API authentication
APIKEY=your-api-key

# Required for GitHub API (increases rate limit)
GITHUB__TOKEN=your-github-token

# Required for AI summaries
GROQ__APIKEY=your-groq-api-key

# Optional: customize the LLM model (default: llama-3.3-70b-versatile)
# GROQ__MODEL=llama-3.3-70b-versatile
```

Get a Groq API key at https://console.groq.com/keys

## Authentication

The app uses [Stytch](https://stytch.com/) for B2C authentication.

### Frontend Configuration

Add these environment variables to the frontend (via `.env` or Vite config):

```bash
VITE_STYTCH_PROJECT_ID=your-project-id
VITE_STYTCH_PUBLIC_TOKEN=your-public-token
```

### Stytch Setup

1. Create a Stytch account at https://stytch.com/
2. Create a new Consumer project
3. Configure allowed redirect URLs for your domains
4. Copy the Project ID and Public Token to your environment

## Quick Start

### 1. Build the backend

```bash
dotnet build
```

### 2. Apply database migrations (SQLite for local dev)

```bash
dotnet ef database update --context SqliteContext --project PatchNotes.Data --startup-project PatchNotes.Api
```

### 3. Seed the database

```bash
cd PatchNotes.Sync
dotnet run -- --seed
```

### 4. Run the API

```bash
cd PatchNotes.Api
dotnet run
```

API available at: http://localhost:5031

### 5. Run the frontend

```bash
cd patchnotes-web
npm install
npm run dev
```

Frontend available at: http://localhost:5173

## Testing

### Test the API endpoints

```bash
# List packages
curl http://localhost:5031/api/packages

# Get releases (last 7 days)
curl http://localhost:5031/api/releases

# Get releases for specific packages
curl "http://localhost:5031/api/releases?packages=react,vue&days=30"

# Add a package
curl -X POST http://localhost:5031/api/packages \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -d '{"npmName": "lodash"}'

# Get AI summary of a release (requires auth)
curl -X POST http://localhost:5031/api/releases/1/summarize \
  -H "Cookie: stytch_session=your-session-token"
```

### Sync CLI

```bash
# Run full sync pipeline (concurrent sync + summary generation)
dotnet run --project PatchNotes.Sync

# Sync a single repo
dotnet run --project PatchNotes.Sync -- -r https://github.com/prettier/prettier

# Generate summaries for a specific package
dotnet run --project PatchNotes.Sync -- -s prettier/prettier

# Seed package catalog from packages.json and sync all from GitHub (first-time setup)
dotnet run --project PatchNotes.Sync -- --init

# Seed database with sample data (local dev)
dotnet run --project PatchNotes.Sync -- --seed
```

Exit codes: 0=success, 1=partial failure, 2=fatal error

The default sync uses a **producer-consumer pipeline** (`SyncPipeline`) — as soon as a package finishes syncing, its summaries start generating while the next package syncs.

## Project Structure

```
PatchNotes/
├── PatchNotes.Api/           # Web API
├── PatchNotes.Data/          # Data layer
│   ├── Migrations/           # EF Core migrations
│   ├── GitHub/               # GitHub API client
│   ├── AI/                   # AI client (OpenAI-compatible)
│   └── SeedData/             # Package catalog (packages.json)
├── PatchNotes.Sync/          # Sync CLI + SyncPipeline
├── PatchNotes.Functions/     # Azure Functions (timer-triggered sync)
├── PatchNotes.Tests/         # xUnit tests
└── patchnotes-web/           # React frontend
    └── src/
        ├── components/
        │   ├── package-picker/   # Package selection UI
        │   ├── releases/         # Release timeline components
        │   └── ui/               # Shared UI components
        ├── pages/                # Route pages
        ├── api/                  # TanStack Query hooks
        └── routes/               # TanStack Router config
```

## Tech Stack

**Backend:**
- .NET 10 / ASP.NET Core
- Entity Framework Core (SQLite dev / SQL Server prod)
- Azure Functions (isolated worker, timer trigger)
- GitHub API integration
- AI summaries (OpenAI-compatible API)

**Frontend:**
- React 18 + TypeScript
- TanStack Router (type-safe routing)
- TanStack Query (data fetching)
- Vite (build tool)

## Development

### Database Migrations

The project uses separate EF Core migrations for SQLite (development) and SQL Server (production).

**Creating a new migration** (when you change entity models):

```bash
# Set SQL Server connection string (required)
export ConnectionStrings__PatchNotes="Server=...;Database=...;User Id=...;Password=..."

# Generate migrations for both providers
./scripts/add-migration.sh MigrationName
```

This creates migrations in:
- `PatchNotes.Data/Migrations/Sqlite/` - Local development
- `PatchNotes.Data/Migrations/SqlServer/` - Production

**Important:** CI will fail if you change models without creating migrations. The `has-pending-model-changes` check ensures migrations are always committed with model changes.

For more details, see [PatchNotes.Data/README.md](PatchNotes.Data/README.md).

### Running Tests

**Backend:**
```bash
dotnet test PatchNotes.slnx
```

**Frontend:**
```bash
cd patchnotes-web
npm test
```

### Code Quality

The project uses GitHub Actions for CI. All PRs must pass:
- `dotnet build` and `dotnet test`
- `npm run lint` and `npm run format:check`
- `npm run build` (includes TypeScript type checking)

## Contributing

### Code Style

**Backend (.NET):**
- Follow standard C# conventions
- Use async/await for I/O operations
- Keep controllers thin, business logic in services

**Frontend (TypeScript):**
- Use TypeScript strict mode
- Prefer TanStack Query for data fetching
- Follow the existing component structure

### Pull Request Process

1. Create a feature branch from `main`
2. Make your changes with clear commit messages
3. Ensure all CI checks pass
4. Request review

## License

MIT
