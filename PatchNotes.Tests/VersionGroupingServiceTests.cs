using FluentAssertions;
using PatchNotes.Data;
using PatchNotes.Sync.Core;

namespace PatchNotes.Tests;

public class VersionGroupingServiceTests
{
    private readonly VersionGroupingService _sut = new();

    private Release MakeRelease(string tag, string packageId = "pkg-1")
    {
        var parsed = VersionParser.ParseTagValues(tag);
        return new Release
        {
            Tag = tag,
            PackageId = packageId,
            PublishedAt = DateTimeOffset.UtcNow,
            FetchedAt = DateTimeOffset.UtcNow,
            MajorVersion = parsed.MajorVersion,
            MinorVersion = parsed.MinorVersion,
            PatchVersion = parsed.PatchVersion,
            IsPrerelease = parsed.IsPrerelease
        };
    }

    #region Standard Semver Parsing

    [Theory]
    [InlineData("1.0.0", 1, false)]
    [InlineData("v1.0.0", 1, false)]
    [InlineData("v15.0.0", 15, false)]
    [InlineData("0.1.0", 0, false)]
    [InlineData("100.200.300", 100, false)]
    public void GroupReleases_StandardSemver_GroupsByMajor(string tag, int expectedMajor, bool expectedPrerelease)
    {
        var releases = new[] { MakeRelease(tag) };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().ContainSingle();
        groups[0].MajorVersion.Should().Be(expectedMajor);
        groups[0].IsPrerelease.Should().Be(expectedPrerelease);
        groups[0].Releases.Should().ContainSingle();
    }

    [Theory]
    [InlineData("v1.0.0-alpha", 1, true)]
    [InlineData("v1.0.0-beta.1", 1, true)]
    [InlineData("v1.0.0-rc.1", 1, true)]
    [InlineData("v16.2.0-canary.3", 16, true)]
    [InlineData("1.0.0-preview.1", 1, true)]
    [InlineData("2.0.0-next.5", 2, true)]
    [InlineData("v1.0.0-nightly", 1, true)]
    [InlineData("v1.0.0-dev", 1, true)]
    public void GroupReleases_PrereleaseTag_MarkedAsPrerelease(string tag, int expectedMajor, bool expectedPrerelease)
    {
        var releases = new[] { MakeRelease(tag) };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().ContainSingle();
        groups[0].MajorVersion.Should().Be(expectedMajor);
        groups[0].IsPrerelease.Should().Be(expectedPrerelease);
    }

    #endregion

    #region Monorepo Tags

    [Theory]
    [InlineData("@scope/package@1.0.0", 1, false)]
    [InlineData("@package/v1.0.0", 1, false)]
    [InlineData("@babel/core@7.23.0", 7, false)]
    [InlineData("react-dom/v18.2.0", 18, false)]
    [InlineData("@types/node@20.10.0", 20, false)]
    public void GroupReleases_MonorepoTag_ExtractsMajor(string tag, int expectedMajor, bool expectedPrerelease)
    {
        var releases = new[] { MakeRelease(tag) };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().ContainSingle();
        groups[0].MajorVersion.Should().Be(expectedMajor);
        groups[0].IsPrerelease.Should().Be(expectedPrerelease);
    }

    [Theory]
    [InlineData("@scope/package@1.0.0-alpha.1", 1, true)]
    [InlineData("@next/mdx@15.0.0-canary.3", 15, true)]
    public void GroupReleases_MonorepoPrerelease_DetectsPrerelease(string tag, int expectedMajor, bool expectedPrerelease)
    {
        var releases = new[] { MakeRelease(tag) };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().ContainSingle();
        groups[0].MajorVersion.Should().Be(expectedMajor);
        groups[0].IsPrerelease.Should().Be(expectedPrerelease);
    }

    #endregion

    #region Simple Semver (MAJOR.MINOR)

    [Theory]
    [InlineData("v5.9", 5, false)]
    [InlineData("15.0", 15, false)]
    [InlineData("v1.0", 1, false)]
    public void GroupReleases_SimpleSemver_ParsesMajor(string tag, int expectedMajor, bool expectedPrerelease)
    {
        var releases = new[] { MakeRelease(tag) };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().ContainSingle();
        groups[0].MajorVersion.Should().Be(expectedMajor);
        groups[0].IsPrerelease.Should().Be(expectedPrerelease);
    }

    [Theory]
    [InlineData("v5.9-rc", 5, true)]
    [InlineData("15.0-beta", 15, true)]
    public void GroupReleases_SimpleSemverPrerelease_DetectsPrerelease(string tag, int expectedMajor, bool expectedPrerelease)
    {
        var releases = new[] { MakeRelease(tag) };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().ContainSingle();
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
    public void GroupReleases_ReleaseStyleTag_ParsesMajor(string tag, int expectedMajor, bool expectedPrerelease)
    {
        var releases = new[] { MakeRelease(tag) };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().ContainSingle();
        groups[0].MajorVersion.Should().Be(expectedMajor);
        groups[0].IsPrerelease.Should().Be(expectedPrerelease);
    }

    #endregion

    #region Non-Standard Prerelease

    [Theory]
    [InlineData("1.0.0beta1", 1, true)]
    [InlineData("1.0.0.rc1", 1, true)]
    [InlineData("2.0.0alpha", 2, true)]
    [InlineData("3.0.0preview", 3, true)]
    public void GroupReleases_NonStandardPrerelease_DetectsPrerelease(string tag, int expectedMajor, bool expectedPrerelease)
    {
        var releases = new[] { MakeRelease(tag) };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().ContainSingle();
        groups[0].MajorVersion.Should().Be(expectedMajor);
        groups[0].IsPrerelease.Should().Be(expectedPrerelease);
    }

    #endregion

    #region Non-Semver Tags (Unversioned)

    [Theory]
    [InlineData("latest")]
    [InlineData("stable")]
    [InlineData("main")]
    [InlineData("nightly-2024-01-15")]
    [InlineData("commit-abc123")]
    [InlineData("")]
    [InlineData("   ")]
    public void GroupReleases_NonSemverTag_GroupedAsUnversioned(string tag)
    {
        var releases = new[] { MakeRelease(tag) };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().ContainSingle();
        groups[0].MajorVersion.Should().Be(-1);
        groups[0].IsPrerelease.Should().BeFalse();
    }

    [Fact]
    public void GroupReleases_MixOfValidAndNonSemver_SeparatesCorrectly()
    {
        var releases = new[]
        {
            MakeRelease("v1.0.0"),
            MakeRelease("latest"),
            MakeRelease("v2.0.0"),
            MakeRelease("nightly-build"),
        };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().HaveCount(3);
        groups.Should().Contain(g => g.MajorVersion == 1 && !g.IsPrerelease);
        groups.Should().Contain(g => g.MajorVersion == 2 && !g.IsPrerelease);
        groups.Should().Contain(g => g.MajorVersion == -1); // unversioned
        groups.First(g => g.MajorVersion == -1).Releases.Should().HaveCount(2);
    }

    #endregion

    #region Grouping by Major Version

    [Fact]
    public void GroupReleases_SameMajor_GroupedTogether()
    {
        var releases = new[]
        {
            MakeRelease("v15.0.0"),
            MakeRelease("v15.1.0"),
            MakeRelease("v15.2.3"),
        };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().ContainSingle();
        groups[0].MajorVersion.Should().Be(15);
        groups[0].IsPrerelease.Should().BeFalse();
        groups[0].Releases.Should().HaveCount(3);
    }

    [Fact]
    public void GroupReleases_DifferentMajors_SeparateGroups()
    {
        var releases = new[]
        {
            MakeRelease("v15.0.0"),
            MakeRelease("v16.0.0"),
            MakeRelease("v17.0.0"),
        };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().HaveCount(3);
        groups.Should().Contain(g => g.MajorVersion == 15);
        groups.Should().Contain(g => g.MajorVersion == 16);
        groups.Should().Contain(g => g.MajorVersion == 17);
    }

    [Fact]
    public void GroupReleases_StableAndPrereleaseSameMajor_SeparateGroups()
    {
        var releases = new[]
        {
            MakeRelease("v16.0.0"),
            MakeRelease("v16.1.0"),
            MakeRelease("v16.0.0-alpha.1"),
            MakeRelease("v16.0.0-beta.1"),
        };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().HaveCount(2);

        var stable = groups.First(g => !g.IsPrerelease);
        stable.MajorVersion.Should().Be(16);
        stable.Releases.Should().HaveCount(2);

        var pre = groups.First(g => g.IsPrerelease);
        pre.MajorVersion.Should().Be(16);
        pre.Releases.Should().HaveCount(2);
    }

    #endregion

    #region Deduplication

    [Fact]
    public void GroupReleases_DuplicateTags_Deduplicates()
    {
        var releases = new[]
        {
            MakeRelease("v1.0.0", "pkg-1"),
            MakeRelease("v1.0.0", "pkg-1"),
            MakeRelease("v1.0.0", "pkg-1"),
        };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().ContainSingle();
        groups[0].Releases.Should().ContainSingle();
    }

    [Fact]
    public void GroupReleases_SameTagDifferentPackages_NotDeduplicated()
    {
        var releases = new[]
        {
            MakeRelease("v1.0.0", "pkg-1"),
            MakeRelease("v1.0.0", "pkg-2"),
        };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().HaveCount(2);
        groups.Should().Contain(g => g.PackageId == "pkg-1");
        groups.Should().Contain(g => g.PackageId == "pkg-2");
    }

    #endregion

    #region Multi-Package Grouping

    [Fact]
    public void GroupReleases_MultiplePackages_GroupedSeparately()
    {
        var releases = new[]
        {
            MakeRelease("v1.0.0", "react"),
            MakeRelease("v1.1.0", "react"),
            MakeRelease("v1.0.0", "vue"),
            MakeRelease("v2.0.0", "vue"),
        };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().HaveCount(3);
        groups.Should().Contain(g => g.PackageId == "react" && g.MajorVersion == 1);
        groups.Should().Contain(g => g.PackageId == "vue" && g.MajorVersion == 1);
        groups.Should().Contain(g => g.PackageId == "vue" && g.MajorVersion == 2);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GroupReleases_EmptyInput_ReturnsEmpty()
    {
        var groups = _sut.GroupReleases([]).ToList();

        groups.Should().BeEmpty();
    }

    [Fact]
    public void GroupReleases_SingleRelease_SingleGroup()
    {
        var releases = new[] { MakeRelease("v1.0.0") };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().ContainSingle();
    }

    [Fact]
    public void GroupReleases_BuildMetadataIgnored_GroupedByVersion()
    {
        var releases = new[]
        {
            MakeRelease("v1.0.0+build123"),
            MakeRelease("v1.0.0+build456"),
        };

        var groups = _sut.GroupReleases(releases).ToList();

        // Different build metadata = different tags, but same major/stable
        groups.Should().ContainSingle();
        groups[0].MajorVersion.Should().Be(1);
        groups[0].IsPrerelease.Should().BeFalse();
        groups[0].Releases.Should().HaveCount(2);
    }

    [Fact]
    public void GroupReleases_PackageIdPreserved()
    {
        var releases = new[] { MakeRelease("v1.0.0", "my-package") };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().ContainSingle();
        groups[0].PackageId.Should().Be("my-package");
    }

    #endregion

    #region Real-World Scenarios

    [Fact]
    public void GroupReleases_NextJsCanaryReleases_GroupedCorrectly()
    {
        var releases = new[]
        {
            MakeRelease("v16.2.0-canary.3", "nextjs"),
            MakeRelease("v16.2.0-canary.2", "nextjs"),
            MakeRelease("v16.2.0-canary.1", "nextjs"),
            MakeRelease("v16.1.0", "nextjs"),
            MakeRelease("v16.0.0", "nextjs"),
            MakeRelease("v15.3.0", "nextjs"),
        };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().HaveCount(3);

        var stable16 = groups.First(g => g.MajorVersion == 16 && !g.IsPrerelease);
        stable16.Releases.Should().HaveCount(2);

        var pre16 = groups.First(g => g.MajorVersion == 16 && g.IsPrerelease);
        pre16.Releases.Should().HaveCount(3);

        var stable15 = groups.First(g => g.MajorVersion == 15 && !g.IsPrerelease);
        stable15.Releases.Should().HaveCount(1);
    }

    [Fact]
    public void GroupReleases_NodeJsVersions_GroupedByMajor()
    {
        var releases = new[]
        {
            MakeRelease("v25.4.0", "nodejs"),
            MakeRelease("v25.3.0", "nodejs"),
            MakeRelease("v24.13.0", "nodejs"),
            MakeRelease("v24.12.0", "nodejs"),
        };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().HaveCount(2);
        groups.First(g => g.MajorVersion == 25).Releases.Should().HaveCount(2);
        groups.First(g => g.MajorVersion == 24).Releases.Should().HaveCount(2);
    }

    [Fact]
    public void GroupReleases_TypeScriptVersions_HandlesRcCorrectly()
    {
        var releases = new[]
        {
            MakeRelease("v5.9.3", "typescript"),
            MakeRelease("v5.9.2", "typescript"),
            MakeRelease("v5.9-rc", "typescript"),
        };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().HaveCount(2);

        var stable = groups.First(g => !g.IsPrerelease);
        stable.MajorVersion.Should().Be(5);
        stable.Releases.Should().HaveCount(2);

        var pre = groups.First(g => g.IsPrerelease);
        pre.MajorVersion.Should().Be(5);
        pre.Releases.Should().HaveCount(1);
    }

    [Fact]
    public void GroupReleases_MixedFormats_HandlesGracefully()
    {
        var releases = new[]
        {
            MakeRelease("v1.0.0", "pkg"),
            MakeRelease("release-2.0.0", "pkg"),
            MakeRelease("@scope/pkg@3.0.0", "pkg"),
            MakeRelease("latest", "pkg"),
            MakeRelease("nightly-2024-01-15", "pkg"),
        };

        var groups = _sut.GroupReleases(releases).ToList();

        groups.Should().HaveCount(4);
        groups.Should().Contain(g => g.MajorVersion == 1);
        groups.Should().Contain(g => g.MajorVersion == 2);
        groups.Should().Contain(g => g.MajorVersion == 3);
        groups.Should().Contain(g => g.MajorVersion == -1);
    }

    [Fact]
    public void GroupReleases_LargeSet_AllGroupedCorrectly()
    {
        var releases = new List<Release>();
        for (var major = 1; major <= 10; major++)
        {
            for (var minor = 0; minor < 5; minor++)
            {
                releases.Add(MakeRelease($"v{major}.{minor}.0"));
            }
            releases.Add(MakeRelease($"v{major}.0.0-beta.1"));
        }

        var groups = _sut.GroupReleases(releases).ToList();

        // 10 stable groups + 10 prerelease groups
        groups.Should().HaveCount(20);
        groups.Where(g => !g.IsPrerelease).Should().HaveCount(10);
        groups.Where(g => g.IsPrerelease).Should().HaveCount(10);

        foreach (var stableGroup in groups.Where(g => !g.IsPrerelease))
        {
            stableGroup.Releases.Should().HaveCount(5);
        }
        foreach (var preGroup in groups.Where(g => g.IsPrerelease))
        {
            preGroup.Releases.Should().HaveCount(1);
        }
    }

    #endregion
}
