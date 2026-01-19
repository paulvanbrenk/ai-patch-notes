# PatchNotes

A GitHub release viewer for npm packages. Track release notes across your favorite packages in one place.

## Project Status

**Stage:** Development (MVP)
**Health Score:** 8/10

| Area | Status |
|------|--------|
| Architecture | ✅ Solid (.NET + React separation) |
| Code Quality | ✅ Good |
| CI/CD | ✅ GitHub Actions configured |
| Testing | ✅ 141 Vitest tests + xUnit API/Sync tests |
| Security | ⚠️ Needs auth before production |
| Error Handling | ✅ Error boundaries + toast notifications |

## Features

- **Package Tracking** - Add npm packages to monitor their GitHub releases
- **Release Timeline** - Mobile-first timeline view grouped by date
- **Package Picker** - Filter releases by selected packages
- **Sync Engine** - Fetch releases from GitHub with rate limit awareness
- **AI Summaries** - Generate concise release note summaries using Groq LLM
- **Design System** - Consistent visual language across components

## Architecture

- **PatchNotes.Data** - EF Core models, SQLite database, GitHub API client, Groq LLM client
- **PatchNotes.Api** - ASP.NET Core Web API (port 5031)
- **PatchNotes.Sync** - Console app to fetch releases from GitHub
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

## Quick Start

### 1. Build the backend

```bash
dotnet build
```

### 2. Apply database migrations

```bash
cd PatchNotes.Data
dotnet ef database update
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

# Get AI summary of a release
curl -X POST http://localhost:5031/api/releases/1/summarize
```

### Run the sync

```bash
cd PatchNotes.Sync
dotnet run
```

Exit codes: 0=success, 1=partial failure, 2=fatal error

### Seed sample data

```bash
cd PatchNotes.Sync
dotnet run -- --seed
```

## Project Structure

```
PatchNotes/
├── PatchNotes.Api/           # Web API
├── PatchNotes.Data/          # Data layer
│   ├── Migrations/           # EF Core migrations
│   ├── GitHub/               # GitHub API client
│   └── Groq/                 # Groq LLM client
├── PatchNotes.Sync/          # Sync console app
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
- Entity Framework Core + SQLite
- GitHub API integration

**Frontend:**
- React 18 + TypeScript
- TanStack Router (type-safe routing)
- TanStack Query (data fetching)
- Vite (build tool)

## Development

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
