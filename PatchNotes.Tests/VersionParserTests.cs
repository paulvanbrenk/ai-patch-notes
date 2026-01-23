using FluentAssertions;
using PatchNotes.Api.Routes;

namespace PatchNotes.Tests;

public class VersionParserTests
{
    #region Standard Semver Parsing

    [Theory]
    [InlineData("1.0.0", 1, 0, 0)]
    [InlineData("v1.0.0", 1, 0, 0)]
    [InlineData("15.0.0", 15, 0, 0)]
    [InlineData("v15.0.0", 15, 0, 0)]
    [InlineData("0.1.0", 0, 1, 0)]
    [InlineData("v0.0.1", 0, 0, 1)]
    [InlineData("100.200.300", 100, 200, 300)]
    public void Parse_StandardSemver_ExtractsVersionNumbers(string tag, int major, int minor, int patch)
    {
        var result = VersionParser.Parse(tag);

        result.Success.Should().BeTrue();
        result.Version!.Major.Should().Be(major);
        result.Version.Minor.Should().Be(minor);
        result.Version.Patch.Should().Be(patch);
        result.Version.IsPrerelease.Should().BeFalse();
    }

    [Theory]
    [InlineData("v1.0.0-alpha", "alpha")]
    [InlineData("v1.0.0-beta", "beta")]
    [InlineData("v1.0.0-rc.1", "rc.1")]
    [InlineData("v1.0.0-alpha.1", "alpha.1")]
    [InlineData("v16.2.0-canary.3", "canary.3")]
    [InlineData("v5.9.0-beta.2", "beta.2")]
    [InlineData("1.0.0-preview.1", "preview.1")]
    [InlineData("2.0.0-next.5", "next.5")]
    public void Parse_WithPrerelease_ExtractsPrereleaseIdentifier(string tag, string expectedPrerelease)
    {
        var result = VersionParser.Parse(tag);

        result.Success.Should().BeTrue();
        result.Version!.Prerelease.Should().Be(expectedPrerelease);
        result.Version.IsPrerelease.Should().BeTrue();
    }

    [Theory]
    [InlineData("v1.0.0+build123", "build123")]
    [InlineData("v1.0.0+20230101", "20230101")]
    [InlineData("v1.0.0-alpha+build", "build")]
    public void Parse_WithBuildMetadata_ExtractsBuildMetadata(string tag, string expectedBuild)
    {
        var result = VersionParser.Parse(tag);

        result.Success.Should().BeTrue();
        result.Version!.BuildMetadata.Should().Be(expectedBuild);
    }

    #endregion

    #region Monorepo Tag Parsing

    [Theory]
    [InlineData("@scope/package@1.0.0", "@scope/package", 1, 0, 0)]
    [InlineData("@package/v1.0.0", "@package", 1, 0, 0)]
    [InlineData("@babel/core@7.23.0", "@babel/core", 7, 23, 0)]
    [InlineData("react-dom/v18.2.0", "react-dom", 18, 2, 0)]
    [InlineData("@types/node@20.10.0", "@types/node", 20, 10, 0)]
    public void Parse_MonorepoTag_ExtractsPackageAndVersion(string tag, string expectedPackage, int major, int minor, int patch)
    {
        var result = VersionParser.Parse(tag);

        result.Success.Should().BeTrue();
        result.Version!.MonorepoPackage.Should().Be(expectedPackage);
        result.Version.Major.Should().Be(major);
        result.Version.Minor.Should().Be(minor);
        result.Version.Patch.Should().Be(patch);
    }

    [Theory]
    [InlineData("@scope/package@1.0.0-alpha.1", "@scope/package", "alpha.1")]
    [InlineData("@next/mdx@15.0.0-canary.3", "@next/mdx", "canary.3")]
    public void Parse_MonorepoTagWithPrerelease_ExtractsAll(string tag, string expectedPackage, string expectedPrerelease)
    {
        var result = VersionParser.Parse(tag);

        result.Success.Should().BeTrue();
        result.Version!.MonorepoPackage.Should().Be(expectedPackage);
        result.Version.Prerelease.Should().Be(expectedPrerelease);
        result.Version.IsPrerelease.Should().BeTrue();
    }

    #endregion

    #region Simple Semver (MAJOR.MINOR only)

    [Theory]
    [InlineData("v5.9", 5, 9, 0)]
    [InlineData("15.0", 15, 0, 0)]
    [InlineData("v1.0", 1, 0, 0)]
    public void Parse_SimpleSemver_ParsesWithZeroPatch(string tag, int major, int minor, int patch)
    {
        var result = VersionParser.Parse(tag);

        result.Success.Should().BeTrue();
        result.Version!.Major.Should().Be(major);
        result.Version.Minor.Should().Be(minor);
        result.Version.Patch.Should().Be(patch);
    }

    [Theory]
    [InlineData("v5.9-rc", "rc")]
    [InlineData("15.0-beta", "beta")]
    public void Parse_SimpleSemverWithPrerelease_Parses(string tag, string expectedPrerelease)
    {
        var result = VersionParser.Parse(tag);

        result.Success.Should().BeTrue();
        result.Version!.Prerelease.Should().Be(expectedPrerelease);
        result.Version.IsPrerelease.Should().BeTrue();
    }

    #endregion

    #region Release-Style Tags

    [Theory]
    [InlineData("release-1.0.0", 1, 0, 0)]
    [InlineData("release-v1.0.0", 1, 0, 0)]
    [InlineData("release/v2.0.0", 2, 0, 0)]
    [InlineData("RELEASE-v3.0.0", 3, 0, 0)]
    public void Parse_ReleaseStyleTag_Parses(string tag, int major, int minor, int patch)
    {
        var result = VersionParser.Parse(tag);

        result.Success.Should().BeTrue();
        result.Version!.Major.Should().Be(major);
        result.Version.Minor.Should().Be(minor);
        result.Version.Patch.Should().Be(patch);
    }

    #endregion

    #region Non-Semver Tags (Should Fail)

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("latest")]
    [InlineData("stable")]
    [InlineData("main")]
    [InlineData("nightly-build")]
    [InlineData("commit-abc123")]
    public void Parse_NonSemverTag_ReturnsFail(string tag)
    {
        var result = VersionParser.Parse(tag);

        result.Success.Should().BeFalse();
        result.Version.Should().BeNull();
        result.Error.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GetMajorVersion

    [Theory]
    [InlineData("v15.0.0", 15)]
    [InlineData("v16.2.0-canary.3", 16)]
    [InlineData("1.0.0", 1)]
    [InlineData("@babel/core@7.23.0", 7)]
    [InlineData("v5.9-rc", 5)]
    public void GetMajorVersion_ValidTag_ReturnsMajor(string tag, int expected)
    {
        var result = VersionParser.GetMajorVersion(tag);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("latest")]
    [InlineData("nightly")]
    [InlineData("")]
    public void GetMajorVersion_InvalidTag_ReturnsNull(string tag)
    {
        var result = VersionParser.GetMajorVersion(tag);

        result.Should().BeNull();
    }

    #endregion

    #region IsPrerelease

    [Theory]
    [InlineData("v1.0.0-alpha", true)]
    [InlineData("v1.0.0-beta.1", true)]
    [InlineData("v1.0.0-rc.1", true)]
    [InlineData("v16.2.0-canary.3", true)]
    [InlineData("v1.0.0-preview", true)]
    [InlineData("v1.0.0-next.5", true)]
    [InlineData("v1.0.0-nightly", true)]
    [InlineData("v1.0.0-dev", true)]
    [InlineData("v1.0.0-experimental", true)]
    public void IsPrerelease_PrereleaseTag_ReturnsTrue(string tag, bool expected)
    {
        var result = VersionParser.IsPrerelease(tag);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("v1.0.0", false)]
    [InlineData("v15.0.0", false)]
    [InlineData("1.2.3", false)]
    [InlineData("@babel/core@7.23.0", false)]
    public void IsPrerelease_StableTag_ReturnsFalse(string tag, bool expected)
    {
        var result = VersionParser.IsPrerelease(tag);

        result.Should().Be(expected);
    }

    // Edge case: keyword in tag but not in prerelease position
    [Theory]
    [InlineData("alphabetical-1.0.0", true)]  // Contains "alpha" - heuristic catches this
    [InlineData("beta-package@1.0.0", true)]  // Contains "beta" - heuristic catches this
    public void IsPrerelease_KeywordInTagName_UsesHeuristic(string tag, bool expected)
    {
        var result = VersionParser.IsPrerelease(tag);

        result.Should().Be(expected);
    }

    #endregion

    #region GetVersionGroupKey

    [Theory]
    [InlineData("v15.0.0", "15.x")]
    [InlineData("v15.1.0", "15.x")]
    [InlineData("v15.0.1", "15.x")]
    [InlineData("v16.0.0", "16.x")]
    [InlineData("v1.0.0", "1.x")]
    public void GetVersionGroupKey_StableVersion_ReturnsMajorX(string tag, string expectedGroup)
    {
        var result = VersionParser.Parse(tag);

        result.Success.Should().BeTrue();
        result.Version!.GetVersionGroupKey().Should().Be(expectedGroup);
    }

    [Theory]
    [InlineData("v16.2.0-canary.3", "16.x-canary")]
    [InlineData("v16.0.0-alpha.1", "16.x-alpha")]
    [InlineData("v16.0.0-beta", "16.x-beta")]
    [InlineData("v16.0.0-rc.1", "16.x-rc")]
    [InlineData("v15.0.0-preview.1", "15.x-preview")]
    public void GetVersionGroupKey_PrereleaseVersion_ReturnsMajorXWithType(string tag, string expectedGroup)
    {
        var result = VersionParser.Parse(tag);

        result.Success.Should().BeTrue();
        result.Version!.GetVersionGroupKey().Should().Be(expectedGroup);
    }

    #endregion

    #region GetPrereleaseType

    [Theory]
    [InlineData("v1.0.0-alpha.1", "alpha")]
    [InlineData("v1.0.0-beta", "beta")]
    [InlineData("v1.0.0-rc.1", "rc")]
    [InlineData("v1.0.0-canary.3", "canary")]
    [InlineData("v1.0.0-preview.1", "preview")]
    [InlineData("v1.0.0-next.5", "next")]
    public void GetPrereleaseType_ExtractsCorrectType(string tag, string expectedType)
    {
        var result = VersionParser.Parse(tag);

        result.Success.Should().BeTrue();
        result.Version!.GetPrereleaseType().Should().Be(expectedType);
    }

    [Fact]
    public void GetPrereleaseType_StableVersion_ReturnsStable()
    {
        var result = VersionParser.Parse("v1.0.0");

        result.Success.Should().BeTrue();
        result.Version!.GetPrereleaseType().Should().Be("stable");
    }

    #endregion

    #region GroupByMajorVersion

    [Fact]
    public void GroupByMajorVersion_MixedVersions_GroupsCorrectly()
    {
        var tags = new[]
        {
            "v15.0.0",
            "v15.1.0",
            "v16.0.0",
            "v16.0.0-canary.1",
            "v16.0.0-canary.2",
            "v16.0.0-beta.1"
        };

        var groups = VersionParser.GroupByMajorVersion(tags);

        groups.Should().HaveCount(4);

        groups["15.x"].MajorVersion.Should().Be(15);
        groups["15.x"].IsPrerelease.Should().BeFalse();
        groups["15.x"].Versions.Should().HaveCount(2);

        groups["16.x"].MajorVersion.Should().Be(16);
        groups["16.x"].IsPrerelease.Should().BeFalse();
        groups["16.x"].Versions.Should().HaveCount(1);

        groups["16.x-canary"].MajorVersion.Should().Be(16);
        groups["16.x-canary"].IsPrerelease.Should().BeTrue();
        groups["16.x-canary"].PrereleaseType.Should().Be("canary");
        groups["16.x-canary"].Versions.Should().HaveCount(2);

        groups["16.x-beta"].MajorVersion.Should().Be(16);
        groups["16.x-beta"].IsPrerelease.Should().BeTrue();
        groups["16.x-beta"].PrereleaseType.Should().Be("beta");
        groups["16.x-beta"].Versions.Should().HaveCount(1);
    }

    [Fact]
    public void GroupByMajorVersion_SkipsInvalidTags()
    {
        var tags = new[] { "v1.0.0", "latest", "v2.0.0", "nightly", "v3.0.0" };

        var groups = VersionParser.GroupByMajorVersion(tags);

        groups.Should().HaveCount(3);
        groups.Keys.Should().BeEquivalentTo(["1.x", "2.x", "3.x"]);
    }

    [Fact]
    public void GroupByMajorVersion_EmptyInput_ReturnsEmptyDictionary()
    {
        var groups = VersionParser.GroupByMajorVersion([]);

        groups.Should().BeEmpty();
    }

    #endregion

    #region GroupAndSort

    [Fact]
    public void GroupAndSort_ReturnsGroupsSortedByMajorDescending()
    {
        var tags = new[] { "v1.0.0", "v3.0.0", "v2.0.0" };

        var groups = VersionParser.GroupAndSort(tags);

        groups.Should().HaveCount(3);
        groups[0].MajorVersion.Should().Be(3);
        groups[1].MajorVersion.Should().Be(2);
        groups[2].MajorVersion.Should().Be(1);
    }

    [Fact]
    public void GroupAndSort_StableBeforePrerelease()
    {
        var tags = new[] { "v16.0.0-canary.1", "v16.0.0", "v16.0.0-alpha.1" };

        var groups = VersionParser.GroupAndSort(tags);

        groups.Should().HaveCount(3);
        groups[0].GroupKey.Should().Be("16.x");
        groups[0].IsPrerelease.Should().BeFalse();
        // Pre-releases follow, sorted alphabetically by type
        groups[1].IsPrerelease.Should().BeTrue();
        groups[2].IsPrerelease.Should().BeTrue();
    }

    #endregion

    #region Real-World Scenarios from Seed Data

    [Fact]
    public void Parse_TypeScriptVersions_ParsesCorrectly()
    {
        var tags = new[] { "v5.9.3", "v5.9.2", "v5.9-rc" };

        foreach (var tag in tags)
        {
            var result = VersionParser.Parse(tag);
            result.Success.Should().BeTrue($"Failed to parse TypeScript tag: {tag}");
        }

        VersionParser.IsPrerelease("v5.9-rc").Should().BeTrue();
        VersionParser.IsPrerelease("v5.9.3").Should().BeFalse();
    }

    [Fact]
    public void Parse_NextJsVersions_ParsesCorrectly()
    {
        var tags = new[] { "v16.2.0-canary.3", "v16.2.0-canary.2", "v16.2.0-canary.1" };

        foreach (var tag in tags)
        {
            var result = VersionParser.Parse(tag);
            result.Success.Should().BeTrue($"Failed to parse Next.js tag: {tag}");
            result.Version!.IsPrerelease.Should().BeTrue();
            result.Version.GetPrereleaseType().Should().Be("canary");
        }
    }

    [Fact]
    public void Parse_NodeJsVersions_ParsesCorrectly()
    {
        var tags = new[] { "v25.4.0", "v25.3.0", "v24.13.0" };

        var groups = VersionParser.GroupByMajorVersion(tags);

        groups.Should().HaveCount(2);
        groups["25.x"].Versions.Should().HaveCount(2);
        groups["24.x"].Versions.Should().HaveCount(1);
    }

    [Fact]
    public void Parse_ReactVersions_ParsesCorrectly()
    {
        var tags = new[] { "v19.1.4", "v19.0.3" };

        var groups = VersionParser.GroupByMajorVersion(tags);

        groups.Should().HaveCount(1);
        groups["19.x"].Versions.Should().HaveCount(2);
    }

    #endregion
}
