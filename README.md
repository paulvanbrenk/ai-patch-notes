# PatchNotes

A GitHub release viewer for npm packages. Track release notes across your favorite packages in one place.

*Forged in Gas Town*

## Deployment

| Environment | URL | Status |
|-------------|-----|--------|
| Frontend | https://myreleasenotes.ai | Live |
| API | https://api.myreleasenotes.ai | Live |
| Sync Function | fn-patchnotes-sync (Azure Functions) | Timer (every 6h) |
| Email Functions | patchnotes-email (Azure Functions) | HTTP + Timer triggers |

## Project Status

**Stage:** Production (MVP)

| Area | Status |
|------|--------|
| Architecture | ✅ .NET API + React SPA + Azure Functions |
| Code Quality | ✅ Good |
| CI/CD | ✅ GitHub Actions (build, test, deploy API + Function + frontend) |
| Testing | ✅ 368 xUnit tests + 118 Vitest tests |
| Authentication | ✅ Stytch B2C |
| Sync | ✅ Concurrent pipeline (Channel-based producer-consumer) |

## Features

- **Package Tracking** - Add npm packages to monitor their GitHub releases
- **Release Timeline** - Mobile-first timeline view grouped by date
- **Feed** - Combined feed with server-side grouping, filtering stable/pre-release
- **Package Picker** - Filter releases by selected packages
- **Sync Engine** - Fetch releases from GitHub with rate limit awareness
- **AI Summaries** - Generate concise release note summaries using Ollama Cloud (gemma3:27b)
- **Watchlist** - Per-user package watchlists with default packages on signup
- **Subscriptions** - Stripe-powered Pro subscriptions
- **Email Notifications** - Welcome, release, and weekly digest emails via Resend
- **Design System** - Consistent visual language across components

## Architecture

- **PatchNotes.Data** - EF Core models, SQLite/SQL Server, database seeding, version parsing
- **PatchNotes.Api** - ASP.NET Core Web API (port 5031), Stytch authentication
- **PatchNotes.Sync** - CLI tool + SyncPipeline for concurrent sync & summary generation, GitHub client, AI client
- **PatchNotes.Functions** - Azure Functions timer trigger that runs the SyncPipeline every 6 hours
- **patchnotes-web** - React frontend with TanStack Router & Query, Orval-generated API client
- **patchnotes-email** - Azure Functions (TypeScript) for email delivery via Resend

## Prerequisites

- .NET 10 SDK
- Node.js 22+
- pnpm 10+
- [direnv](https://direnv.net/) (recommended for secrets management)

## Configuration

The project uses direnv for environment-based configuration. Create a secrets file at `~/.secrets/patchnotes/.env.local`:

```bash
# Required for API authentication
APIKEY=your-api-key

# Required for GitHub API (increases rate limit)
GITHUB__TOKEN=your-github-token

# Required for AI summaries (Ollama Cloud)
AI__APIKEY=your-ollama-api-key
AI__BASEURL=https://ollama.com/v1/
AI__MODEL=gemma3:27b
```

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
pnpm install
pnpm dev
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

# Get combined feed
curl http://localhost:5031/api/feed

# Get packages by owner
curl http://localhost:5031/api/packages/facebook

# Get package by owner/repo
curl http://localhost:5031/api/packages/facebook/react

# Add a package
curl -X POST http://localhost:5031/api/packages \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -d '{"npmName": "lodash"}'
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
├── PatchNotes.Api/           # Web API + Stytch auth
│   ├── Routes/               # Minimal API route handlers
│   ├── Stytch/               # Stytch authentication client
│   └── Webhooks/             # Stytch + Stripe webhook handlers
├── PatchNotes.Data/          # Data layer
│   ├── Migrations/           # EF Core migrations (Sqlite + SqlServer)
│   └── SeedData/             # Package catalog (packages.json)
├── PatchNotes.Sync/          # Sync CLI + SyncPipeline
│   ├── GitHub/               # GitHub API client
│   └── AI/                   # AI client (OpenAI-compatible)
├── PatchNotes.Functions/     # Azure Functions (timer-triggered sync)
├── PatchNotes.Tests/         # xUnit tests
├── patchnotes-web/           # React frontend
│   └── src/
│       ├── api/              # Orval-generated + custom hooks
│       ├── components/       # UI components
│       ├── pages/            # Route pages
│       └── routes/           # TanStack Router config
└── patchnotes-email/         # Azure Functions (email via Resend)
    └── src/
        ├── functions/        # sendWelcome, sendRelease, sendDigest
        └── lib/              # Resend client
```

## Tech Stack

**Backend:**
- .NET 10 / ASP.NET Core
- Entity Framework Core (SQLite dev / SQL Server prod)
- Azure Functions (isolated worker, timer trigger)
- GitHub API integration
- AI summaries via Ollama Cloud (gemma3:27b, OpenAI-compatible API)
- Stytch B2C authentication
- Stripe subscriptions

**Frontend:**
- React 19 + TypeScript
- TanStack Router (type-safe file-based routing)
- TanStack Query (data fetching)
- Tailwind CSS 4
- Vite 7
- Orval (OpenAPI client generation)

**Email:**
- Azure Functions (TypeScript)
- Resend (email delivery)

**Workspace:**
- pnpm 10 workspace monorepo (`patchnotes-web`, `patchnotes-email`)

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
pnpm test
```

### Code Quality

The project uses GitHub Actions for CI. All PRs must pass:
- `dotnet build` and `dotnet test`
- `pnpm lint` and `pnpm format:check`
- `pnpm build` (includes TypeScript type checking)

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
