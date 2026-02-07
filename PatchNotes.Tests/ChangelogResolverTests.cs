using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PatchNotes.Data.GitHub;
using PatchNotes.Sync;

namespace PatchNotes.Tests;

public class ChangelogResolverTests
{
    private readonly Mock<IGitHubClient> _mockGitHub;
    private readonly Mock<ILogger<ChangelogResolver>> _mockLogger;
    private readonly ChangelogResolver _resolver;

    public ChangelogResolverTests()
    {
        _mockGitHub = new Mock<IGitHubClient>();
        _mockLogger = new Mock<ILogger<ChangelogResolver>>();
        _resolver = new ChangelogResolver(_mockGitHub.Object, _mockLogger.Object);
    }

    #region IsChangelogReference Tests

    [Fact]
    public void IsChangelogReference_NullBody_ReturnsTrue()
    {
        ChangelogResolver.IsChangelogReference(null).Should().BeTrue();
    }

    [Fact]
    public void IsChangelogReference_EmptyBody_ReturnsTrue()
    {
        ChangelogResolver.IsChangelogReference("").Should().BeTrue();
    }

    [Fact]
    public void IsChangelogReference_WhitespaceBody_ReturnsTrue()
    {
        ChangelogResolver.IsChangelogReference("   ").Should().BeTrue();
    }

    [Theory]
    [InlineData("Please refer to CHANGELOG.md for details")]
    [InlineData("See CHANGELOG.md for full details")]
    [InlineData("See HISTORY.md for details")]
    [InlineData("See CHANGES.md for details")]
    [InlineData("Full changelog: https://github.com/owner/repo/compare/v1.0.0...v2.0.0")]
    public void IsChangelogReference_ShortReferenceBody_ReturnsTrue(string body)
    {
        ChangelogResolver.IsChangelogReference(body).Should().BeTrue();
    }

    [Fact]
    public void IsChangelogReference_LongBody_ReturnsFalse()
    {
        var body = new string('x', 100);
        ChangelogResolver.IsChangelogReference(body).Should().BeFalse();
    }

    [Fact]
    public void IsChangelogReference_ShortButNoReference_ReturnsFalse()
    {
        ChangelogResolver.IsChangelogReference("Bug fixes and improvements").Should().BeFalse();
    }

    [Fact]
    public void IsChangelogReference_LongBodyWithChangelogMention_ReturnsFalse()
    {
        // Body is over 100 chars, even though it mentions CHANGELOG.md
        var body = "This release includes many improvements. " + new string('x', 60) + " See CHANGELOG.md";
        ChangelogResolver.IsChangelogReference(body).Should().BeFalse();
    }

    #endregion

    #region ExtractVersionSection Tests

    [Fact]
    public void ExtractVersionSection_MatchesExactVersion()
    {
        var changelog = """
            # Changelog

            ## 2.0.0

            Breaking changes here.

            ## 1.0.0

            Initial release.
            """;

        var result = ChangelogResolver.ExtractVersionSection(changelog, "v1.0.0");
        result.Should().Be("Initial release.");
    }

    [Fact]
    public void ExtractVersionSection_MatchesBracketedVersion()
    {
        var changelog = """
            # Changelog

            ## [1.2.3]

            Some fixes.

            ## [1.2.2]

            Other fixes.
            """;

        var result = ChangelogResolver.ExtractVersionSection(changelog, "v1.2.3");
        result.Should().Be("Some fixes.");
    }

    [Fact]
    public void ExtractVersionSection_MatchesVersionWithVPrefix()
    {
        var changelog = """
            # Changelog

            ## v1.0.0

            Content here.

            ## v0.9.0

            Old content.
            """;

        var result = ChangelogResolver.ExtractVersionSection(changelog, "v1.0.0");
        result.Should().Be("Content here.");
    }

    [Fact]
    public void ExtractVersionSection_MatchesVersionWithDateSuffix()
    {
        var changelog = """
            # Changelog

            ### 1.2.3 (2024-01-15)

            Dated release notes.

            ### 1.2.2 (2024-01-10)

            Older notes.
            """;

        var result = ChangelogResolver.ExtractVersionSection(changelog, "1.2.3");
        result.Should().StartWith("Dated release notes.");
    }

    [Fact]
    public void ExtractVersionSection_ReturnsNull_WhenVersionNotFound()
    {
        var changelog = """
            # Changelog

            ## 1.0.0

            Content.
            """;

        var result = ChangelogResolver.ExtractVersionSection(changelog, "v2.0.0");
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractVersionSection_ReturnsNull_WhenNoHeadings()
    {
        var changelog = "Just some text without any headings.";

        var result = ChangelogResolver.ExtractVersionSection(changelog, "v1.0.0");
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractVersionSection_ReturnsNull_WhenSectionEmpty()
    {
        var changelog = """
            ## 1.0.0

            ## 0.9.0

            Content here.
            """;

        var result = ChangelogResolver.ExtractVersionSection(changelog, "v1.0.0");
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractVersionSection_ExtractsToEndOfFile_WhenLastSection()
    {
        var changelog = """
            ## 2.0.0

            First section.

            ## 1.0.0

            Last section content.
            With multiple lines.
            """;

        var result = ChangelogResolver.ExtractVersionSection(changelog, "v1.0.0");
        result.Should().Contain("Last section content.");
        result.Should().Contain("With multiple lines.");
    }

    [Fact]
    public void ExtractVersionSection_StopsAtSameLevelHeading()
    {
        var changelog = """
            ## 2.0.0

            ### Bug Fixes

            - Fixed something.

            ### Features

            - Added something.

            ## 1.0.0

            Initial.
            """;

        var result = ChangelogResolver.ExtractVersionSection(changelog, "v2.0.0");
        result.Should().Contain("Fixed something.");
        result.Should().Contain("Added something.");
        result.Should().NotContain("Initial.");
    }

    #endregion

    #region ResolveAsync Tests

    [Fact]
    public async Task ResolveAsync_FetchesChangelogAndExtractsSection()
    {
        var changelog = """
            ## 2.0.0

            New features.

            ## 1.0.0

            Initial release.
            """;

        _mockGitHub
            .Setup(x => x.GetFileContentAsync("owner", "repo", "CHANGELOG.md", It.IsAny<CancellationToken>()))
            .ReturnsAsync(changelog);

        var result = await _resolver.ResolveAsync("owner", "repo", "v1.0.0");

        result.Should().Be("Initial release.");
    }

    [Fact]
    public async Task ResolveAsync_TriesFallbackPaths()
    {
        var changelog = """
            ## 1.0.0

            Found in CHANGES.md.
            """;

        _mockGitHub
            .Setup(x => x.GetFileContentAsync("owner", "repo", "CHANGELOG.md", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _mockGitHub
            .Setup(x => x.GetFileContentAsync("owner", "repo", "CHANGES.md", It.IsAny<CancellationToken>()))
            .ReturnsAsync(changelog);

        var result = await _resolver.ResolveAsync("owner", "repo", "v1.0.0");

        result.Should().Be("Found in CHANGES.md.");
    }

    [Fact]
    public async Task ResolveAsync_ReturnsNull_WhenNoChangelogFound()
    {
        _mockGitHub
            .Setup(x => x.GetFileContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await _resolver.ResolveAsync("owner", "repo", "v1.0.0");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_ReturnsNull_WhenVersionNotInChangelog()
    {
        var changelog = """
            ## 2.0.0

            Only this version.
            """;

        _mockGitHub
            .Setup(x => x.GetFileContentAsync("owner", "repo", "CHANGELOG.md", It.IsAny<CancellationToken>()))
            .ReturnsAsync(changelog);

        var result = await _resolver.ResolveAsync("owner", "repo", "v1.0.0");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_HandlesExceptionsGracefully()
    {
        _mockGitHub
            .Setup(x => x.GetFileContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API error"));

        var result = await _resolver.ResolveAsync("owner", "repo", "v1.0.0");

        result.Should().BeNull();
    }

    #endregion
}
