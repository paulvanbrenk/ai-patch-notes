# PatchNotes

A web application for tracking and browsing release notes from npm packages. Stay up to date with changes in your favorite libraries by aggregating GitHub release information in one place.

## Features

- Track npm packages and their GitHub release notes
- Browse release history with version tags and publish dates
- Sync release notes from GitHub repositories
- Search and filter releases across packages

## Project Structure

```
PatchNotes.sln
├── PatchNotes.Api/        # ASP.NET Core Web API
├── PatchNotes.Data/       # Entity Framework Core data layer (SQLite)
├── PatchNotes.Sync/       # Package release sync service
└── patchnotes-web/        # React frontend
```

## Tech Stack

**Backend:**
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQLite

**Frontend:**
- React 19
- TypeScript
- Vite
- Tailwind CSS
- React Router

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)

### Backend

```bash
# Restore dependencies and run the API
cd PatchNotes.Api
dotnet run
```

The API will start at `https://localhost:5001` (or the port configured in launchSettings.json).

### Frontend

```bash
# Install dependencies
cd patchnotes-web
npm install

# Start development server
npm run dev
```

The frontend will start at `http://localhost:5173`.

### Database

The application uses SQLite with EF Core migrations. To apply migrations:

```bash
cd PatchNotes.Data
dotnet ef database update
```

## Data Model

- **Package** - Represents a tracked npm package with its GitHub repository info
  - `NpmName` - npm package name (e.g., `react`, `lodash`)
  - `GithubOwner` / `GithubRepo` - GitHub repository coordinates
  - `LastFetchedAt` - Timestamp of last sync
- **Release** - Contains release notes for a specific version of a package
  - `Tag` - Version tag (e.g., `v1.0.0`)
  - `Title` / `Body` - Release title and markdown content
  - `PublishedAt` - When the release was published on GitHub

## API Endpoints

The API exposes the following endpoints (at `https://localhost:5001`):

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/packages` | List all tracked packages |
| GET | `/api/packages/{id}` | Get package details with releases |
| POST | `/api/packages` | Add a new package to track |
| DELETE | `/api/packages/{id}` | Remove a tracked package |
| GET | `/api/releases` | List recent releases across all packages |
| POST | `/api/sync/{packageId}` | Trigger sync for a specific package |

## Development

```bash
# Build the entire solution
dotnet build

# Run frontend linting
cd patchnotes-web && npm run lint

# Build frontend for production
cd patchnotes-web && npm run build
```

## License

MIT
