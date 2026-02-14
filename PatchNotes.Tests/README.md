# PatchNotes.Tests

Test suite for the PatchNotes backend.

## Overview

This project contains unit and integration tests for the API, data layer, and sync service using xUnit. Currently **368 tests**.

## Running Tests

```bash
dotnet test
```

Or from the solution root:

```bash
dotnet test PatchNotes.slnx
```

### With Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Categories

- **API Tests** - Integration tests for REST endpoints (`PackagesApiTests`, `ReleasesApiTests`, `FeedApiTests`, `WatchlistApiTests`, `EmailPreferencesTests`)
- **Unit Tests** - Entity and business logic tests (`PackageTests`, `ReleaseTests`, `VersionParserTests`, `DefaultWatchlistOptionsTests`)
- **Service Tests** - Sync and summary service tests (`SyncServiceTests`, `SummaryGenerationServiceTests`, `VersionGroupingServiceTests`)
- **Client Tests** - GitHub client and URL parsing tests (`GitHubClientTests`, `GitHubUrlParserTests`, `ChangelogResolverTests`)
- **User Tests** - User and watchlist tests (`UserWatchlistTests`)

## Test Infrastructure

- **PatchNotesApiFixture** - WebApplicationFactory setup for API integration tests
- **SQLite** - Uses EF Core SQLite provider for test isolation
- **Moq** - Mocking framework for unit tests
- **FluentAssertions** - Readable assertion syntax

## Dependencies

- xUnit
- FluentAssertions
- Moq
- Microsoft.AspNetCore.Mvc.Testing
- Microsoft.EntityFrameworkCore.Sqlite
