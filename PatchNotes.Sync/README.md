# PatchNotes.Sync

Console application for syncing GitHub releases for tracked packages.

## Overview

This project fetches release information from GitHub for all tracked npm packages and stores them in the database. It's designed to run as a scheduled job (cron) or manually.

## Usage

### Sync All Packages

```bash
dotnet run
```

Fetches releases for all tracked packages from GitHub.

### Seed Sample Data

```bash
dotnet run -- --seed
```

Seeds the database with sample packages for development/testing.

## Exit Codes

The application returns exit codes suitable for cron monitoring:

| Code | Meaning |
|------|---------|
| 0 | Success - all packages synced |
| 1 | Partial failure - some packages failed |
| 2 | Fatal error - sync could not complete |

## Configuration

Configuration is loaded from `appsettings.json` and environment variables:

- `GitHub:Token` - GitHub personal access token (increases API rate limit from 60 to 5000 requests/hour)

## Components

- **Program.cs** - Entry point with CLI argument parsing
- **SyncService.cs** - Core sync logic for fetching and storing releases

## Dependencies

- **PatchNotes.Data** - Database context and GitHub client
