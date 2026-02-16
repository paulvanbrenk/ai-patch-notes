using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PatchNotes.Data;

namespace PatchNotes.Tests;

public class PackagesApiTests : IAsyncLifetime
{
    private PatchNotesApiFixture _fixture = null!;
    private HttpClient _client = null!;
    private HttpClient _authClient = null!;
    private HttpClient _nonAdminClient = null!;

    public async Task InitializeAsync()
    {
        _fixture = new PatchNotesApiFixture();
        await _fixture.InitializeAsync();
        _client = _fixture.CreateClient();
        _authClient = _fixture.CreateAuthenticatedClient();
        _nonAdminClient = _fixture.CreateNonAdminClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        _authClient.Dispose();
        _nonAdminClient.Dispose();
        await _fixture.DisposeAsync();
        _fixture.Dispose();
    }

    #region GET /api/packages

    [Fact]
    public async Task GetPackages_ReturnsEmptyList_WhenNoPackages()
    {
        var response = await _client.GetAsync("/api/packages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("items").GetArrayLength().Should().Be(0);
        result.GetProperty("total").GetInt32().Should().Be(0);
        result.GetProperty("limit").GetInt32().Should().Be(20);
        result.GetProperty("offset").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetPackages_ReturnsAllPackages()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        db.Packages.AddRange(
            new Package { Name = "react", Url = "https://github.com/facebook/react", NpmName = "react", GithubOwner = "facebook", GithubRepo = "react" },
            new Package { Name = "vue", Url = "https://github.com/vuejs/core", NpmName = "vue", GithubOwner = "vuejs", GithubRepo = "core" }
        );
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/packages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("items").GetArrayLength().Should().Be(2);
        result.GetProperty("total").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task GetPackages_ReturnsCorrectFields()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var createdAt = DateTimeOffset.UtcNow;
        db.Packages.Add(new Package
        {
            Name = "lodash",
            Url = "https://github.com/lodash/lodash",
            NpmName = "lodash",
            GithubOwner = "lodash",
            GithubRepo = "lodash",
            LastFetchedAt = createdAt.AddHours(1)
        });
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/packages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var pkg = result.GetProperty("items")[0];
        pkg.GetProperty("npmName").GetString().Should().Be("lodash");
        pkg.GetProperty("githubOwner").GetString().Should().Be("lodash");
        pkg.GetProperty("githubRepo").GetString().Should().Be("lodash");
        pkg.TryGetProperty("id", out _).Should().BeTrue();
        pkg.TryGetProperty("createdAt", out _).Should().BeTrue();
        pkg.TryGetProperty("lastFetchedAt", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetPackages_RespectsLimitAndOffset()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        db.Packages.AddRange(
            new Package { Name = "alpha", Url = "https://github.com/o/alpha", NpmName = "alpha", GithubOwner = "o", GithubRepo = "alpha" },
            new Package { Name = "bravo", Url = "https://github.com/o/bravo", NpmName = "bravo", GithubOwner = "o", GithubRepo = "bravo" },
            new Package { Name = "charlie", Url = "https://github.com/o/charlie", NpmName = "charlie", GithubOwner = "o", GithubRepo = "charlie" }
        );
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/packages?limit=2&offset=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("items").GetArrayLength().Should().Be(2);
        result.GetProperty("total").GetInt32().Should().Be(3);
        result.GetProperty("limit").GetInt32().Should().Be(2);
        result.GetProperty("offset").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetPackages_ClampsLimitToMax100()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        db.Packages.Add(new Package { Name = "pkg", Url = "https://github.com/o/r", NpmName = "pkg", GithubOwner = "o", GithubRepo = "r" });
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/packages?limit=500");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("limit").GetInt32().Should().Be(100);
    }

    #endregion

    #region POST /api/packages

    [Fact]
    public async Task PostPackage_RequiresAuthentication()
    {
        var response = await _client.PostAsJsonAsync("/api/packages", new { npmName = "test" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostPackage_ReturnsBadRequest_WhenNpmNameMissing()
    {
        var response = await _authClient.PostAsJsonAsync("/api/packages", new { npmName = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        error.GetProperty("error").GetString().Should().Be("npmName is required");
    }

    [Fact]
    public async Task PostPackage_ReturnsNotFound_WhenPackageNotOnNpm()
    {
        _fixture.NpmHandler.SetupPackageNotFound("nonexistent-package-xyz");

        var response = await _authClient.PostAsJsonAsync("/api/packages", new { npmName = "nonexistent-package-xyz" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        error.GetProperty("error").GetString().Should().Be("Package not found on npm");
    }

    [Fact]
    public async Task PostPackage_ReturnsBadRequest_WhenPackageHasNoGitHubRepo()
    {
        _fixture.NpmHandler.SetupPackageWithoutRepo("package-without-repo");

        var response = await _authClient.PostAsJsonAsync("/api/packages", new { npmName = "package-without-repo" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        error.GetProperty("error").GetString().Should().Be("Package does not have a GitHub repository");
    }

    [Fact]
    public async Task PostPackage_CreatesPackage_WhenValid()
    {
        _fixture.NpmHandler.SetupPackage("express", "expressjs", "express");

        var response = await _authClient.PostAsJsonAsync("/api/packages", new { npmName = "express" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var pkg = await response.Content.ReadFromJsonAsync<JsonElement>();
        pkg.GetProperty("npmName").GetString().Should().Be("express");
        pkg.GetProperty("githubOwner").GetString().Should().Be("expressjs");
        pkg.GetProperty("githubRepo").GetString().Should().Be("express");
        pkg.TryGetProperty("id", out var id).Should().BeTrue();
        id.GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PostPackage_ReturnsConflict_WhenPackageAlreadyExists()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        db.Packages.Add(new Package
        {
            Name = "duplicate-pkg",
            Url = "https://github.com/owner/repo",
            NpmName = "duplicate-pkg",
            GithubOwner = "owner",
            GithubRepo = "repo",
        });
        await db.SaveChangesAsync();

        // Act
        _fixture.NpmHandler.SetupPackage("duplicate-pkg", "other", "repo");
        var response = await _authClient.PostAsJsonAsync("/api/packages", new { npmName = "duplicate-pkg" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        error.GetProperty("error").GetString().Should().Be("Package already exists");
    }

    #endregion

    #region DELETE /api/packages/{id}

    [Fact]
    public async Task DeletePackage_RequiresAuthentication()
    {
        var response = await _client.DeleteAsync("/api/packages/nonexistent-id-1234xx");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeletePackage_ReturnsNotFound_WhenPackageDoesNotExist()
    {
        var response = await _authClient.DeleteAsync("/api/packages/nonexistent-id-1234xx");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePackage_DeletesPackage_WhenExists()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var pkg = new Package
        {
            Name = "to-delete",
            Url = "https://github.com/owner/repo",
            NpmName = "to-delete",
            GithubOwner = "owner",
            GithubRepo = "repo",
        };
        db.Packages.Add(pkg);
        await db.SaveChangesAsync();
        var id = pkg.Id;

        // Act
        var response = await _authClient.DeleteAsync($"/api/packages/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await _client.GetAsync("/api/packages");
        var result = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    #endregion

    #region GET /api/packages/{owner}

    [Fact]
    public async Task GetPackagesByOwner_ReturnsEmptyList_WhenNoPackagesForOwner()
    {
        var response = await _client.GetAsync("/api/packages/nonexistent-owner");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("items").GetArrayLength().Should().Be(0);
        result.GetProperty("total").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetPackagesByOwner_ReturnsMatchingPackages()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        db.Packages.AddRange(
            new Package { Name = ".NET Runtime", Url = "https://github.com/dotnet/runtime", NpmName = "dotnet-runtime", GithubOwner = "dotnet", GithubRepo = "runtime" },
            new Package { Name = "EF Core", Url = "https://github.com/dotnet/efcore", NpmName = "dotnet-efcore", GithubOwner = "dotnet", GithubRepo = "efcore" },
            new Package { Name = "React", Url = "https://github.com/facebook/react", NpmName = "react", GithubOwner = "facebook", GithubRepo = "react" }
        );
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/packages/dotnet");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("items").GetArrayLength().Should().Be(2);
        result.GetProperty("total").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task GetPackagesByOwner_ReturnsCorrectFields()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var pkg = new Package { Name = "FastAPI", Url = "https://github.com/tiangolo/fastapi", NpmName = "fastapi", GithubOwner = "tiangolo", GithubRepo = "fastapi" };
        db.Packages.Add(pkg);
        db.Releases.Add(new Release { PackageId = pkg.Id, Tag = "v0.100.0", PublishedAt = DateTimeOffset.UtcNow, FetchedAt = DateTimeOffset.UtcNow, MajorVersion = 0, MinorVersion = 100, PatchVersion = 0 });
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/packages/tiangolo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var p = result.GetProperty("items")[0];
        p.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
        p.GetProperty("name").GetString().Should().Be("FastAPI");
        p.GetProperty("githubOwner").GetString().Should().Be("tiangolo");
        p.GetProperty("githubRepo").GetString().Should().Be("fastapi");
        p.GetProperty("latestVersion").GetString().Should().Be("v0.100.0");
        p.TryGetProperty("lastUpdated", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetPackagesByOwner_RespectsLimitAndOffset()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        db.Packages.AddRange(
            new Package { Name = "alpha", Url = "https://github.com/testowner/alpha", NpmName = "alpha", GithubOwner = "testowner", GithubRepo = "alpha" },
            new Package { Name = "bravo", Url = "https://github.com/testowner/bravo", NpmName = "bravo", GithubOwner = "testowner", GithubRepo = "bravo" },
            new Package { Name = "charlie", Url = "https://github.com/testowner/charlie", NpmName = "charlie", GithubOwner = "testowner", GithubRepo = "charlie" }
        );
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/packages/testowner?limit=1&offset=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("items").GetArrayLength().Should().Be(1);
        result.GetProperty("total").GetInt32().Should().Be(3);
        result.GetProperty("limit").GetInt32().Should().Be(1);
        result.GetProperty("offset").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task PostPackage_ReturnsForbidden_WhenNonAdmin()
    {
        _fixture.NpmHandler.SetupPackage("forbidden-pkg", "owner", "repo");

        var response = await _nonAdminClient.PostAsJsonAsync("/api/packages", new { npmName = "forbidden-pkg" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region PATCH /api/packages/{id}

    [Fact]
    public async Task PatchPackage_ReturnsForbidden_WhenNonAdmin()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var pkg = new Package { Name = "test", Url = "https://github.com/o/r", NpmName = "test", GithubOwner = "o", GithubRepo = "r" };
        db.Packages.Add(pkg);
        await db.SaveChangesAsync();

        var response = await _nonAdminClient.PatchAsJsonAsync($"/api/packages/{pkg.Id}", new { githubOwner = "new-owner" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region DELETE /api/packages/{id}

    [Fact]
    public async Task DeletePackage_ReturnsForbidden_WhenNonAdmin()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var pkg = new Package { Name = "nodelete", Url = "https://github.com/o/r", NpmName = "nodelete", GithubOwner = "o", GithubRepo = "r" };
        db.Packages.Add(pkg);
        await db.SaveChangesAsync();

        var response = await _nonAdminClient.DeleteAsync($"/api/packages/{pkg.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET /api/packages/{owner}/{repo}

    [Fact]
    public async Task GetPackageByOwnerRepo_ReturnsNotFound_WhenPackageDoesNotExist()
    {
        var response = await _client.GetAsync("/api/packages/nonexistent/repo");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        error.GetProperty("error").GetString().Should().Be("Package not found");
    }

    [Fact]
    public async Task GetPackageByOwnerRepo_ReturnsPackageWithGroups()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var pkg = new Package { Name = ".NET Runtime", Url = "https://github.com/dotnet/runtime", NpmName = "dotnet-runtime", GithubOwner = "dotnet", GithubRepo = "runtime" };
        db.Packages.Add(pkg);
        db.Releases.AddRange(
            new Release { PackageId = pkg.Id, Tag = "v9.0.0", Title = "v9.0.0", Body = "# Release notes for v9", PublishedAt = DateTimeOffset.UtcNow.AddDays(-10), FetchedAt = DateTimeOffset.UtcNow, MajorVersion = 9, MinorVersion = 0, PatchVersion = 0, IsPrerelease = false },
            new Release { PackageId = pkg.Id, Tag = "v9.0.1", Title = "v9.0.1", Body = "# Patch notes", PublishedAt = DateTimeOffset.UtcNow.AddDays(-5), FetchedAt = DateTimeOffset.UtcNow, MajorVersion = 9, MinorVersion = 0, PatchVersion = 1, IsPrerelease = false },
            new Release { PackageId = pkg.Id, Tag = "v10.0.0-preview.1", Title = "v10 Preview", Body = "# Preview notes", PublishedAt = DateTimeOffset.UtcNow.AddDays(-1), FetchedAt = DateTimeOffset.UtcNow, MajorVersion = 10, MinorVersion = 0, PatchVersion = 0, IsPrerelease = true }
        );
        db.ReleaseSummaries.Add(new ReleaseSummary { PackageId = pkg.Id, MajorVersion = 9, IsPrerelease = false, Summary = "AI summary for v9", GeneratedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/packages/dotnet/runtime");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Check package info
        var package = result.GetProperty("package");
        package.GetProperty("name").GetString().Should().Be(".NET Runtime");
        package.GetProperty("githubOwner").GetString().Should().Be("dotnet");
        package.GetProperty("githubRepo").GetString().Should().Be("runtime");

        // Check groups
        var groups = result.GetProperty("groups");
        groups.GetArrayLength().Should().Be(2); // v9 stable + v10 prerelease

        // Find the v9 stable group
        var v9Group = groups.EnumerateArray().First(g => g.GetProperty("majorVersion").GetInt32() == 9);
        v9Group.GetProperty("isPrerelease").GetBoolean().Should().BeFalse();
        v9Group.GetProperty("versionRange").GetString().Should().Be("v9.x");
        v9Group.GetProperty("summary").GetString().Should().Be("AI summary for v9");
        v9Group.GetProperty("releaseCount").GetInt32().Should().Be(2);
        v9Group.GetProperty("releases").GetArrayLength().Should().Be(2);

        // Check releases include body (full markdown)
        var firstRelease = v9Group.GetProperty("releases")[0];
        firstRelease.TryGetProperty("body", out var body).Should().BeTrue();
        body.GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPackageByOwnerRepo_ReturnsAllHistoricGroups()
    {
        // Arrange - multiple major versions including old ones
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var pkg = new Package { Name = "TestPkg", Url = "https://github.com/test/pkg", NpmName = "test-pkg", GithubOwner = "test", GithubRepo = "pkg" };
        db.Packages.Add(pkg);
        db.Releases.AddRange(
            new Release { PackageId = pkg.Id, Tag = "v7.0.0", PublishedAt = DateTimeOffset.UtcNow.AddDays(-365), FetchedAt = DateTimeOffset.UtcNow, MajorVersion = 7, IsPrerelease = false },
            new Release { PackageId = pkg.Id, Tag = "v8.0.0", PublishedAt = DateTimeOffset.UtcNow.AddDays(-180), FetchedAt = DateTimeOffset.UtcNow, MajorVersion = 8, IsPrerelease = false },
            new Release { PackageId = pkg.Id, Tag = "v9.0.0", PublishedAt = DateTimeOffset.UtcNow.AddDays(-30), FetchedAt = DateTimeOffset.UtcNow, MajorVersion = 9, IsPrerelease = false }
        );
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/packages/test/pkg");

        // Assert - should return ALL version groups, not filtered like feed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("groups").GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task GetPackageByOwnerRepo_GroupsWithoutSummary_HaveNullSummary()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var pkg = new Package { Name = "NoSummary", Url = "https://github.com/ns/pkg", NpmName = "ns-pkg", GithubOwner = "ns", GithubRepo = "pkg" };
        db.Packages.Add(pkg);
        db.Releases.Add(new Release { PackageId = pkg.Id, Tag = "v1.0.0", PublishedAt = DateTimeOffset.UtcNow, FetchedAt = DateTimeOffset.UtcNow, MajorVersion = 1, IsPrerelease = false });
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/packages/ns/pkg");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var group = result.GetProperty("groups")[0];
        group.GetProperty("summary").ValueKind.Should().Be(JsonValueKind.Null);
    }

    #endregion

    #region POST /api/packages/bulk

    [Fact]
    public async Task BulkCreatePackages_RequiresAuthentication()
    {
        var response = await _client.PostAsJsonAsync("/api/packages/bulk", new[]
        {
            new { githubOwner = "facebook", githubRepo = "react" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BulkCreatePackages_ReturnsForbidden_WhenNonAdmin()
    {
        var response = await _nonAdminClient.PostAsJsonAsync("/api/packages/bulk", new[]
        {
            new { githubOwner = "facebook", githubRepo = "react" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BulkCreatePackages_ReturnsBadRequest_WhenEmptyArray()
    {
        var response = await _authClient.PostAsJsonAsync("/api/packages/bulk", Array.Empty<object>());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        error.GetProperty("error").GetString().Should().Be("At least one package is required");
    }

    [Fact]
    public async Task BulkCreatePackages_CreatesMultiplePackages()
    {
        var response = await _authClient.PostAsJsonAsync("/api/packages/bulk", new[]
        {
            new { githubOwner = "facebook", githubRepo = "react", name = "React", npmName = "react" },
            new { githubOwner = "vuejs", githubRepo = "core", name = "Vue", npmName = "vue" },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var results = result.GetProperty("results");
        results.GetArrayLength().Should().Be(2);
        results[0].GetProperty("success").GetBoolean().Should().BeTrue();
        results[0].GetProperty("package").GetProperty("githubOwner").GetString().Should().Be("facebook");
        results[1].GetProperty("success").GetBoolean().Should().BeTrue();
        results[1].GetProperty("package").GetProperty("githubOwner").GetString().Should().Be("vuejs");
    }

    [Fact]
    public async Task BulkCreatePackages_DefaultsNameToOwnerRepo()
    {
        var response = await _authClient.PostAsJsonAsync("/api/packages/bulk", new[]
        {
            new { githubOwner = "owner1", githubRepo = "repo1" },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var pkg = result.GetProperty("results")[0].GetProperty("package");
        pkg.GetProperty("name").GetString().Should().Be("owner1/repo1");
    }

    [Fact]
    public async Task BulkCreatePackages_HandlesExistingPackages()
    {
        // Arrange - seed an existing package
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        db.Packages.Add(new Package { Name = "existing", Url = "https://github.com/exists/pkg", NpmName = "existing", GithubOwner = "exists", GithubRepo = "pkg" });
        await db.SaveChangesAsync();

        // Act
        var response = await _authClient.PostAsJsonAsync("/api/packages/bulk", new[]
        {
            new { githubOwner = "exists", githubRepo = "pkg" },
            new { githubOwner = "newowner", githubRepo = "newrepo" },
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var results = result.GetProperty("results");
        results.GetArrayLength().Should().Be(2);

        results[0].GetProperty("success").GetBoolean().Should().BeFalse();
        results[0].GetProperty("error").GetString().Should().Be("Package already exists");

        results[1].GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task BulkCreatePackages_HandlesInvalidEntries()
    {
        var response = await _authClient.PostAsJsonAsync("/api/packages/bulk", new[]
        {
            new { githubOwner = "", githubRepo = "repo" },
            new { githubOwner = "valid", githubRepo = "repo" },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var results = result.GetProperty("results");

        results[0].GetProperty("success").GetBoolean().Should().BeFalse();
        results[0].GetProperty("error").GetString().Should().Be("githubOwner and githubRepo are required");

        results[1].GetProperty("success").GetBoolean().Should().BeTrue();
    }

    #endregion
}
