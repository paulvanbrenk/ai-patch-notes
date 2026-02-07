using FluentAssertions;
using PatchNotes.Data;
using PatchNotes.Sync;

namespace PatchNotes.Tests;

public class VersionGroupingServiceTests
{
    private readonly VersionGroupingService _service = new();

    private static Release CreateRelease(string tag, string packageId = "pkg-1") =>
        new()
        {
            Tag = tag,
            PackageId = packageId,
            PublishedAt = DateTime.UtcNow,
            FetchedAt = DateTime.UtcNow,
        };

    #region Basic Grouping

    [Fact]
    public void GroupReleases_GroupsByMajorVersion()
    {
        var releases = new[]
        {
            CreateRelease("v15.0.0"),
            CreateRelease("v15.1.0"),
            CreateRelease("v16.0.0"),
        };

        var groups = _service.GroupReleases(releases).ToList();

        groups.Should().HaveCount(2);

        var v15 = groups.Single(g => g.MajorVersion == 15);
        v15.Releases.Should().HaveCount(2);
        v15.IsPrerelease.Should().BeFalse();

        var v16 = groups.Single(g => g.MajorVersion == 16);
        v16.Releases.Should().HaveCount(1);
        v16.IsPrerelease.Should().BeFalse();
    }

    [Fact]
    public void GroupReleases_SeparatesPrereleaseFromStable()
    {
        var releases = new[]
        {
            CreateRelease("v16.0.0"),
            CreateRelease("v16.1.0"),
            CreateRelease("v16.0.0-beta.1"),
            CreateRelease("v16.0.0-canary.3"),
        };

        var groups = _service.GroupReleases(releases).ToList();

        groups.Should().HaveCount(2);

        var stable = groups.Single(g => g.MajorVersion == 16 && !g.IsPrerelease);
        stable.Releases.Should().HaveCount(2);

        var prerelease = groups.Single(g => g.MajorVersion == 16 && g.IsPrerelease);
        prerelease.Releases.Should().HaveCount(2);
    }

    [Fact]
    public void GroupReleases_GroupsByPackageId()
    {
        var releases = new[]
        {
            CreateRelease("v1.0.0", "pkg-a"),
            CreateRelease("v1.0.0", "pkg-b"),
            CreateRelease("v1.1.0", "pkg-a"),
        };

        var groups = _service.GroupReleases(releases).ToList();

        groups.Should().HaveCount(2);

        var pkgA = groups.Single(g => g.PackageId == "pkg-a");
        pkgA.Releases.Should().HaveCount(2);
        pkgA.MajorVersion.Should().Be(1);

        var pkgB = groups.Single(g => g.PackageId == "pkg-b");
        pkgB.Releases.Should().HaveCount(1);
    }

    [Fact]
    public void GroupReleases_EmptyInput_ReturnsEmpty()
    {
        var groups = _service.GroupReleases([]).ToList();

        groups.Should().BeEmpty();
    }

    #endregion

    #region Semver Parsing

    [Theory]
    [InlineData("v1.0.0", 1, false)]
    [InlineData("1.0.0", 1, false)]
    [InlineData("v15.0.0", 15, false)]
    [InlineData("v0.1.0", 0, false)]
    [InlineData("100.200.300", 100, false)]
    public void GroupReleases_StandardSemver_ParsesCorrectly(string tag, int expectedMajor, bool expectedPrerelease)
    {
        var releases = new[] { CreateRelease(tag) };

        var groups = _service.GroupReleases(releases).ToList();

        groups.Should().HaveCount(1);
        groups[0].MajorVersion.Should().Be(expectedMajor);
        groups[0].IsPrerelease.Should().Be(expectedPrerelease);
    }

    [Theory]
    [InlineData("v1.0.0-alpha", 1, true)]
    [InlineData("v1.0.0-beta.1", 1, true)]
    [InlineData("v1.0.0-rc.1", 1, true)]
    [InlineData("v16.2.0-canary.3", 16, true)]
    [InlineData("v1.0.0-preview.1", 1, true)]
    [InlineData("v1.0.0-next.5", 1, true)]
    [InlineData("v1.0.0-nightly", 1, true)]
    [InlineData("v1.0.0-dev", 1, true)]
    [InlineData("v1.0.0-experimental", 1, true)]
    public void GroupReleases_Prerelease_DetectsCorrectly(string tag, int expectedMajor, bool expectedPrerelease)
    {
        var releases = new[] { CreateRelease(tag) };

        var groups = _service.GroupReleases(releases).ToList();

        groups.Should().HaveCount(1);
        groups[0].MajorVersion.Should().Be(expectedMajor);
        groups[0].IsPrerelease.Should().Be(expectedPrerelease);
    }

    #endregion

    #region Monorepo Tags

    [Theory]
    [InlineData("@scope/package@1.0.0", 1, false)]
    [InlineData("@babel/core@7.23.0", 7, false)]
    [InlineData("@types/node@20.10.0", 20, false)]
    [InlineData("react-dom/v18.2.0", 18, false)]
    [InlineData("@package/v1.0.0", 1, false)]
    public void GroupReleases_MonorepoTags_ParsesCorrectly(string tag, int expectedMajor, bool expectedPrerelease)
    {
        var releases = new[] { CreateRelease(tag) };

        var groups = _service.GroupReleases(releases).ToList();

        groups.Should().HaveCount(1);
        groups[0].MajorVersion.Should().Be(expectedMajor);
        groups[0].IsPrerelease.Should().Be(expectedPrerelease);
    }

    [Theory]
    [InlineData("@scope/package@1.0.0-alpha.1", 1, true)]
    [InlineData("@next/mdx@15.0.0-canary.3", 15, true)]
    public void GroupReleases_MonorepoPrerelease_DetectsCorrectly(string tag, int expectedMajor, bool expectedPrerelease)
    {
        var releases = new[] { CreateRelease(tag) };

        var groups = _service.GroupReleases(releases).ToList();

        groups.Should().HaveCount(1);
        groups[0].MajorVersion.Should().Be(expectedMajor);
        groups[0].IsPrerelease.Should().Be(expectedPrerelease);
    }

    #endregion

    #region Simple Semver (MAJOR.MINOR only)

    [Theory]
    [InlineData("v5.9", 5, false)]
    [InlineData("15.0", 15, false)]
    [InlineData("v5.9-rc", 5, true)]
    [InlineData("15.0-beta", 15, true)]
    public void GroupReleases_SimpleSemver_ParsesCorrectly(string tag, int expectedMajor, bool expectedPrerelease)
    {
        var releases = new[] { CreateRelease(tag) };

        var groups = _service.GroupReleases(releases).ToList();

        groups.Should().HaveCount(1);
        groups[0].MajorVersion.Should().Be(expectedMajor);
        groups[0].IsPrerelease.Should().Be(expectedPrerelease);
    }

    #endregion

    #region Release-Style Tags

    [Theory]
    [InlineData("release-1.0.0", 1, false)]
    [InlineData("release-v1.0.0", 1, false)]
    [InlineData("release/v2.0.0", 2, false)]
    [InlineData("RELEASE-v3.0.0", 3, false)]
    public void GroupReleases_ReleaseStyleTags_ParsesCorrectly(string tag, int expectedMajor, bool expectedPrerelease)
    {
        var releases = new[] { CreateRelease(tag) };

        var groups = _service.GroupReleases(releases).ToList();

        groups.Should().HaveCount(1);
        groups[0].MajorVersion.Should().Be(expectedMajor);
        groups[0].IsPrerelease.Should().Be(expectedPrerelease);
    }

    #endregion

    #region Non-Standard Pre-release Patterns

    [Theory]
    [InlineData("1.0.0beta1", 1, true)]
    [InlineData("1.0.0.rc1", 1, true)]
    [InlineData("2.0.0alpha", 2, true)]
    [InlineData("3.0.0.beta.2", 3, true)]
    public void GroupReleases_NonStandardPrerelease_DetectsCorrectly(string tag, int expectedMajor, bool expectedPrerelease)
    {
        var releases = new[] { CreateRelease(tag) };

        var groups = _service.GroupReleases(releases).ToList();

        groups.Should().HaveCount(1);
        groups[0].MajorVersion.Should().Be(expectedMajor);
        groups[0].IsPrerelease.Should().Be(expectedPrerelease);
    }

    #endregion

    #region Non-Semver Tags (Unversioned)

    [Theory]
    [InlineData("nightly-2024-01-15")]
    [InlineData("latest")]
    [InlineData("stable")]
    [InlineData("main")]
    [InlineData("commit-abc123")]
    public void GroupReleases_NonSemverTags_GroupsAsUnversioned(string tag)
    {
        var releases = new[] { CreateRelease(tag) };

        var groups = _service.GroupReleases(releases).ToList();

        groups.Should().HaveCount(1);
        groups[0].MajorVersion.Should().Be(-1, "non-semver tags should have MajorVersion -1");
        groups[0].IsPrerelease.Should().BeFalse();
    }

    [Fact]
    public void GroupReleases_MultipleNonSemverTags_GroupedTogether()
    {
        var releases = new[]
        {
            CreateRelease("latest"),
            CreateRelease("nightly-2024-01-15"),
            CreateRelease("stable"),
        };

        var groups = _service.GroupReleases(releases).ToList();

        groups.Should().HaveCount(1);
        groups[0].MajorVersion.Should().Be(-1);
        groups[0].Releases.Should().HaveCount(3);
    }

    #endregion

    #region Deduplication

    [Fact]
    public void GroupReleases_DuplicateTags_Deduplicates()
    {
        var releases = new[]
        {
            CreateRelease("v1.0.0"),
            CreateRelease("v1.0.0"),
            CreateRelease("v1.0.0"),
        };

        var groups = _service.GroupReleases(releases).ToList();

        groups.Should().HaveCount(1);
        groups[0].Releases.Should().HaveCount(1);
    }

    [Fact]
    public void GroupReleases_SameTagDifferentPackages_NotDeduplicated()
    {
        var releases = new[]
        {
            CreateRelease("v1.0.0", "pkg-a"),
            CreateRelease("v1.0.0", "pkg-b"),
        };

        var groups = _service.GroupReleases(releases).ToList();

        groups.Should().HaveCount(2);
        groups.Should().OnlyContain(g => g.Releases.Count == 1);
    }

    #endregion

    #region Mixed Real-World Scenarios

    [Fact]
    public void GroupReleases_MixedVersionsAndPackages_GroupsCorrectly()
    {
        var releases = new[]
        {
            CreateRelease("v15.0.0", "nextjs"),
            CreateRelease("v15.1.0", "nextjs"),
            CreateRelease("v16.0.0-canary.1", "nextjs"),
            CreateRelease("v16.0.0-canary.2", "nextjs"),
            CreateRelease("v16.0.0", "nextjs"),
            CreateRelease("v5.9.3", "typescript"),
            CreateRelease("v5.9-rc", "typescript"),
            CreateRelease("v19.1.4", "react"),
            CreateRelease("latest", "react"),
        };

        var groups = _service.GroupReleases(releases).ToList();

        // nextjs: 15.x stable, 16.x prerelease, 16.x stable = 3 groups
        var nextjsGroups = groups.Where(g => g.PackageId == "nextjs").ToList();
        nextjsGroups.Should().HaveCount(3);

        var nextjs15 = nextjsGroups.Single(g => g.MajorVersion == 15 && !g.IsPrerelease);
        nextjs15.Releases.Should().HaveCount(2);

        var nextjs16Stable = nextjsGroups.Single(g => g.MajorVersion == 16 && !g.IsPrerelease);
        nextjs16Stable.Releases.Should().HaveCount(1);

        var nextjs16Pre = nextjsGroups.Single(g => g.MajorVersion == 16 && g.IsPrerelease);
        nextjs16Pre.Releases.Should().HaveCount(2);

        // typescript: 5.x stable, 5.x prerelease = 2 groups
        var tsGroups = groups.Where(g => g.PackageId == "typescript").ToList();
        tsGroups.Should().HaveCount(2);

        // react: 19.x stable, -1 unversioned = 2 groups
        var reactGroups = groups.Where(g => g.PackageId == "react").ToList();
        reactGroups.Should().HaveCount(2);
        reactGroups.Should().Contain(g => g.MajorVersion == 19 && !g.IsPrerelease);
        reactGroups.Should().Contain(g => g.MajorVersion == -1);
    }

    [Fact]
    public void GroupReleases_AllPrereleaseTypes_DetectedCorrectly()
    {
        var tags = new[]
        {
            "v1.0.0-alpha", "v1.0.0-beta.1", "v1.0.0-canary.3",
            "v1.0.0-preview.1", "v1.0.0-rc.1", "v1.0.0-next.5",
            "v1.0.0-nightly", "v1.0.0-dev", "v1.0.0-experimental",
        };

        var releases = tags.Select(t => CreateRelease(t)).ToArray();

        var groups = _service.GroupReleases(releases).ToList();

        // All should be in the same prerelease group (same package, major=1, prerelease=true)
        groups.Should().HaveCount(1);
        groups[0].IsPrerelease.Should().BeTrue();
        groups[0].MajorVersion.Should().Be(1);
        groups[0].Releases.Should().HaveCount(9);
    }

    [Fact]
    public void GroupReleases_NodeJsVersions_GroupsByMajor()
    {
        var releases = new[]
        {
            CreateRelease("v25.4.0"),
            CreateRelease("v25.3.0"),
            CreateRelease("v24.13.0"),
        };

        var groups = _service.GroupReleases(releases).ToList();

        groups.Should().HaveCount(2);
        groups.Single(g => g.MajorVersion == 25).Releases.Should().HaveCount(2);
        groups.Single(g => g.MajorVersion == 24).Releases.Should().HaveCount(1);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GroupReleases_EmptyTag_GroupsAsUnversioned()
    {
        var releases = new[] { CreateRelease("") };

        var groups = _service.GroupReleases(releases).ToList();

        groups.Should().HaveCount(1);
        groups[0].MajorVersion.Should().Be(-1);
    }

    [Fact]
    public void GroupReleases_WhitespaceTag_GroupsAsUnversioned()
    {
        var releases = new[] { CreateRelease("   ") };

        var groups = _service.GroupReleases(releases).ToList();

        groups.Should().HaveCount(1);
        groups[0].MajorVersion.Should().Be(-1);
    }

    [Fact]
    public void GroupReleases_TagWithBuildMetadata_IgnoresMetadata()
    {
        var releases = new[]
        {
            CreateRelease("v1.0.0+build123"),
            CreateRelease("v1.0.0+build456"),
        };

        var groups = _service.GroupReleases(releases).ToList();

        // Both should be in major=1, not prerelease
        // But they have different tags, so both kept (not deduplicated)
        groups.Should().HaveCount(1);
        groups[0].MajorVersion.Should().Be(1);
        groups[0].IsPrerelease.Should().BeFalse();
        groups[0].Releases.Should().HaveCount(2);
    }

    [Fact]
    public void GroupReleases_PreservesPackageIdFromRelease()
    {
        var releases = new[] { CreateRelease("v1.0.0", "my-special-package") };

        var groups = _service.GroupReleases(releases).ToList();

        groups.Should().HaveCount(1);
        groups[0].PackageId.Should().Be("my-special-package");
    }

    #endregion
}
