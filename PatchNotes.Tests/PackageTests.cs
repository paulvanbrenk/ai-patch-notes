using PatchNotes.Data;

namespace PatchNotes.Tests;

public class PackageTests
{
    [Fact]
    public void Package_WhenCreated_InitializesWithEmptyReleasesList()
    {
        var package = new Package
        {
            Name = "test-package",
            Url = "https://github.com/owner/repo",
            NpmName = "test-package",
            GithubOwner = "owner",
            GithubRepo = "repo"
        };

        Assert.NotNull(package.Releases);
        Assert.Empty(package.Releases);
    }

    [Fact]
    public void Package_GivenReleasesAdded_ContainsThoseReleases()
    {
        var package = new Package
        {
            Name = "test-package",
            Url = "https://github.com/owner/repo",
            NpmName = "test-package",
            GithubOwner = "owner",
            GithubRepo = "repo"
        };

        var release = new Release
        {
            PackageId = package.Id,
            Tag = "v1.0.0",
            PublishedAt = DateTimeOffset.UtcNow,
            FetchedAt = DateTimeOffset.UtcNow,
            Package = package
        };

        package.Releases.Add(release);

        Assert.Single(package.Releases);
        Assert.Equal("v1.0.0", package.Releases.First().Tag);
    }

    [Fact]
    public void Package_LastFetchedAt_DefaultsToNull()
    {
        var package = new Package
        {
            Name = "test-package",
            Url = "https://github.com/owner/repo",
            NpmName = "test-package",
            GithubOwner = "owner",
            GithubRepo = "repo"
        };

        Assert.Null(package.LastFetchedAt);
    }
}
