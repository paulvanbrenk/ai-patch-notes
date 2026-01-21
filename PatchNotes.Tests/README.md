# PatchNotes.Tests

Test suite for the PatchNotes backend.

## Overview

This project contains unit and integration tests for the API, data layer, and sync service using xUnit.

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

- **API Tests** - Integration tests for REST endpoints (`PackagesApiTests`, `ReleasesApiTests`, `NotificationsApiTests`)
- **Unit Tests** - Entity and business logic tests (`PackageTests`, `ReleaseTests`)
- **Service Tests** - Sync service tests (`SyncServiceTests`)
- **Client Tests** - GitHub client tests (`GitHubClientTests`)

## Test Infrastructure

- **PatchNotesApiFixture** - WebApplicationFactory setup for API integration tests
- **In-Memory Database** - Uses EF Core InMemory provider for isolation
- **Moq** - Mocking framework for unit tests
- **FluentAssertions** - Readable assertion syntax

## Dependencies

- xUnit
- FluentAssertions
- Moq
- Microsoft.AspNetCore.Mvc.Testing
- Microsoft.EntityFrameworkCore.InMemory
