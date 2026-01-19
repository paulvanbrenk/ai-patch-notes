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

    public async Task InitializeAsync()
    {
        _fixture = new PatchNotesApiFixture();
        await _fixture.InitializeAsync();
        _client = _fixture.CreateClient();
        _authClient = _fixture.CreateAuthenticatedClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        _authClient.Dispose();
        await _fixture.DisposeAsync();
        _fixture.Dispose();
    }

    #region GET /api/packages

    [Fact]
    public async Task GetPackages_ReturnsEmptyList_WhenNoPackages()
    {
        var response = await _client.GetAsync("/api/packages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var packages = await response.Content.ReadFromJsonAsync<JsonElement>();
        packages.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetPackages_ReturnsAllPackages()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        db.Packages.AddRange(
            new Package { NpmName = "react", GithubOwner = "facebook", GithubRepo = "react", CreatedAt = DateTime.UtcNow },
            new Package { NpmName = "vue", GithubOwner = "vuejs", GithubRepo = "core", CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/packages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var packages = await response.Content.ReadFromJsonAsync<JsonElement>();
        packages.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GetPackages_ReturnsCorrectFields()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var createdAt = DateTime.UtcNow;
        db.Packages.Add(new Package
        {
            NpmName = "lodash",
            GithubOwner = "lodash",
            GithubRepo = "lodash",
            CreatedAt = createdAt,
            LastFetchedAt = createdAt.AddHours(1)
        });
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/packages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var packages = await response.Content.ReadFromJsonAsync<JsonElement>();
        var pkg = packages[0];
        pkg.GetProperty("npmName").GetString().Should().Be("lodash");
        pkg.GetProperty("githubOwner").GetString().Should().Be("lodash");
        pkg.GetProperty("githubRepo").GetString().Should().Be("lodash");
        pkg.TryGetProperty("id", out _).Should().BeTrue();
        pkg.TryGetProperty("createdAt", out _).Should().BeTrue();
        pkg.TryGetProperty("lastFetchedAt", out _).Should().BeTrue();
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
    public async Task PostPackage_ReturnsUnauthorized_WithInvalidApiKey()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "invalid-key");

        var response = await client.PostAsJsonAsync("/api/packages", new { npmName = "test" });

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
        id.GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PostPackage_ReturnsConflict_WhenPackageAlreadyExists()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        db.Packages.Add(new Package
        {
            NpmName = "duplicate-pkg",
            GithubOwner = "owner",
            GithubRepo = "repo",
            CreatedAt = DateTime.UtcNow
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
        var response = await _client.DeleteAsync("/api/packages/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeletePackage_ReturnsNotFound_WhenPackageDoesNotExist()
    {
        var response = await _authClient.DeleteAsync("/api/packages/999");

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
            NpmName = "to-delete",
            GithubOwner = "owner",
            GithubRepo = "repo",
            CreatedAt = DateTime.UtcNow
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
        var packages = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        packages.GetArrayLength().Should().Be(0);
    }

    #endregion
}
