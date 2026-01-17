# PatchNotes

A full-stack application for tracking and viewing release notes from npm packages via their GitHub releases.

## Overview

PatchNotes helps developers stay up-to-date with the latest releases of their dependencies. Add npm packages to your tracking list and the app automatically fetches release information from GitHub, providing a unified view of recent updates.

## Tech Stack

**Backend** (.NET 10)
- ASP.NET Core Web API
- Entity Framework Core with SQLite
- GitHub API integration

**Frontend**
- React 19 with TypeScript
- Vite build tool
- Tailwind CSS

## Project Structure

```
├── PatchNotes.Api/      # ASP.NET Core Web API
├── PatchNotes.Data/     # EF Core models, DbContext, GitHub client
├── PatchNotes.Sync/     # Release sync service
└── patchnotes-web/      # React frontend
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js 20+

### Backend

```bash
cd PatchNotes.Api
dotnet run
```

The API runs database migrations and seeds sample data automatically in development mode.

### Frontend

```bash
cd patchnotes-web
npm install
npm run dev
```

## Features

- Track npm packages by their GitHub repository
- Automatically fetch release notes from GitHub
- View recent releases across all tracked packages
- Search packages and releases
