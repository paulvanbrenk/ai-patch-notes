using PatchNotes.Data;

namespace PatchNotes.Tests;

public class ReleaseTests
{
    [Fact]
    public void Release_TitleAndBody_AreNullable()
    {
        var release = new Release
        {
            Tag = "v1.0.0",
            PublishedAt = DateTime.UtcNow,
            FetchedAt = DateTime.UtcNow
        };

        Assert.Null(release.Title);
        Assert.Null(release.Body);
    }

    [Fact]
    public void Release_CanSetTitleAndBody()
    {
        var release = new Release
        {
            Tag = "v1.0.0",
            Title = "Initial Release",
            Body = "This is the first release",
            PublishedAt = DateTime.UtcNow,
            FetchedAt = DateTime.UtcNow
        };

        Assert.Equal("Initial Release", release.Title);
        Assert.Equal("This is the first release", release.Body);
    }

    [Fact]
    public void Release_RequiresTag()
    {
        var release = new Release
        {
            Tag = "v2.0.0",
            PublishedAt = DateTime.UtcNow,
            FetchedAt = DateTime.UtcNow
        };

        Assert.Equal("v2.0.0", release.Tag);
    }

    [Fact]
    public void NeedsSummary_NullSummary_ReturnsTrue()
    {
        var release = new Release
        {
            Tag = "v1.0.0",
            PublishedAt = DateTime.UtcNow,
            FetchedAt = DateTime.UtcNow,
            Summary = null,
            SummaryStale = false
        };

        Assert.True(release.NeedsSummary);
    }

    [Fact]
    public void NeedsSummary_NullSummaryAndStale_ReturnsTrue()
    {
        var release = new Release
        {
            Tag = "v1.0.0",
            PublishedAt = DateTime.UtcNow,
            FetchedAt = DateTime.UtcNow,
            Summary = null,
            SummaryStale = true
        };

        Assert.True(release.NeedsSummary);
    }

    [Fact]
    public void NeedsSummary_HasSummaryAndStale_ReturnsTrue()
    {
        var release = new Release
        {
            Tag = "v1.0.0",
            PublishedAt = DateTime.UtcNow,
            FetchedAt = DateTime.UtcNow,
            Summary = "A summary",
            SummaryStale = true
        };

        Assert.True(release.NeedsSummary);
    }

    [Fact]
    public void NeedsSummary_HasSummaryNotStale_ReturnsFalse()
    {
        var release = new Release
        {
            Tag = "v1.0.0",
            PublishedAt = DateTime.UtcNow,
            FetchedAt = DateTime.UtcNow,
            Summary = "A summary",
            SummaryStale = false
        };

        Assert.False(release.NeedsSummary);
    }

    [Fact]
    public void SummaryStale_DefaultsToTrue()
    {
        var release = new Release
        {
            Tag = "v1.0.0",
            PublishedAt = DateTime.UtcNow,
            FetchedAt = DateTime.UtcNow
        };

        Assert.True(release.SummaryStale);
    }
}
