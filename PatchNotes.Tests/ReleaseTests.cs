using PatchNotes.Data;

namespace PatchNotes.Tests;

public class ReleaseTests
{
    [Fact]
    public void Release_TitleAndBody_AreNullable()
    {
        var release = new Release
        {
            PackageId = "test-package-id",
            Tag ="v1.0.0",
            PublishedAt = DateTimeOffset.UtcNow,
            FetchedAt = DateTimeOffset.UtcNow
        };

        Assert.Null(release.Title);
        Assert.Null(release.Body);
    }

    [Fact]
    public void Release_GivenTitleAndBodySet_StoresThemCorrectly()
    {
        var release = new Release
        {
            PackageId = "test-package-id",
            Tag ="v1.0.0",
            Title = "Initial Release",
            Body = "This is the first release",
            PublishedAt = DateTimeOffset.UtcNow,
            FetchedAt = DateTimeOffset.UtcNow
        };

        Assert.Equal("Initial Release", release.Title);
        Assert.Equal("This is the first release", release.Body);
    }

    [Fact]
    public void Release_GivenTagSet_StoresTagCorrectly()
    {
        var release = new Release
        {
            PackageId = "test-package-id",
            Tag ="v2.0.0",
            PublishedAt = DateTimeOffset.UtcNow,
            FetchedAt = DateTimeOffset.UtcNow
        };

        Assert.Equal("v2.0.0", release.Tag);
    }

    [Fact]
    public void Release_WhenCreated_SummaryStaleDefaultsToTrue()
    {
        var release = new Release
        {
            PackageId = "test-package-id",
            Tag ="v1.0.0",
            PublishedAt = DateTimeOffset.UtcNow,
            FetchedAt = DateTimeOffset.UtcNow
        };

        Assert.True(release.SummaryStale);
    }
}
