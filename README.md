# PatchNotes

A GitHub release viewer for npm packages. Track release notes across your favorite packages in one place.

## Architecture

- **PatchNotes.Data** - EF Core models, SQLite database, GitHub API client
- **PatchNotes.Api** - ASP.NET Core Web API (port 5031)
- **PatchNotes.Sync** - Console app to fetch releases from GitHub
- **patchnotes-web** - React frontend (Vite)

## Prerequisites

- .NET 10 SDK
- Node.js 18+

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

### 3. Seed the database (optional)

```bash
cd PatchNotes.Api
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
  -d '{"npmName": "lodash"}'
```

### Run the sync

```bash
cd PatchNotes.Sync
dotnet run
```

Exit codes: 0=success, 1=partial failure, 2=fatal error

### Run frontend tests

```bash
cd patchnotes-web
npm test
```

## Project Structure

```
PatchNotes/
├── PatchNotes.Api/        # Web API
├── PatchNotes.Data/       # Data layer (EF Core, GitHub client)
│   ├── Migrations/        # EF Core migrations
│   └── GitHub/            # GitHub API client
├── PatchNotes.Sync/       # Sync console app
└── patchnotes-web/        # React frontend
```

## License

MIT
