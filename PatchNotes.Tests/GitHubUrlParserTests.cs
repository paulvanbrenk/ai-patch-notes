using FluentAssertions;
using PatchNotes.Sync.Core;

namespace PatchNotes.Tests;

public class GitHubUrlParserTests
{
    [Theory]
    [InlineData("https://github.com/prettier/prettier", "prettier", "prettier")]
    [InlineData("https://github.com/facebook/react", "facebook", "react")]
    [InlineData("https://github.com/Microsoft/TypeScript", "Microsoft", "TypeScript")]
    [InlineData("https://github.com/owner/repo.git", "owner", "repo")]
    [InlineData("https://github.com/owner/repo/", "owner", "repo")]
    [InlineData("https://github.com/owner/repo/tree/main", "owner", "repo")]
    public void Parse_WithFullUrl_ExtractsOwnerAndRepo(string url, string expectedOwner, string expectedRepo)
    {
        var (owner, repo) = GitHubUrlParser.Parse(url);

        owner.Should().Be(expectedOwner);
        repo.Should().Be(expectedRepo);
    }

    [Theory]
    [InlineData("prettier/prettier", "prettier", "prettier")]
    [InlineData("facebook/react", "facebook", "react")]
    [InlineData("owner/repo.git", "owner", "repo")]
    public void Parse_WithShorthand_ExtractsOwnerAndRepo(string url, string expectedOwner, string expectedRepo)
    {
        var (owner, repo) = GitHubUrlParser.Parse(url);

        owner.Should().Be(expectedOwner);
        repo.Should().Be(expectedRepo);
    }

    [Theory]
    [InlineData("github:owner/repo", "owner", "repo")]
    [InlineData("github:facebook/react", "facebook", "react")]
    [InlineData("github:owner/repo.git", "owner", "repo")]
    public void Parse_WithGitHubShorthand_ExtractsOwnerAndRepo(string url, string expectedOwner, string expectedRepo)
    {
        var (owner, repo) = GitHubUrlParser.Parse(url);

        owner.Should().Be(expectedOwner);
        repo.Should().Be(expectedRepo);
    }

    [Theory]
    [InlineData("git+https://github.com/owner/repo.git", "owner", "repo")]
    [InlineData("git+https://github.com/facebook/react", "facebook", "react")]
    public void Parse_WithGitPlusHttps_ExtractsOwnerAndRepo(string url, string expectedOwner, string expectedRepo)
    {
        var (owner, repo) = GitHubUrlParser.Parse(url);

        owner.Should().Be(expectedOwner);
        repo.Should().Be(expectedRepo);
    }

    [Theory]
    [InlineData("git://github.com/owner/repo.git", "owner", "repo")]
    [InlineData("git://github.com/facebook/react", "facebook", "react")]
    public void Parse_WithGitProtocol_ExtractsOwnerAndRepo(string url, string expectedOwner, string expectedRepo)
    {
        var (owner, repo) = GitHubUrlParser.Parse(url);

        owner.Should().Be(expectedOwner);
        repo.Should().Be(expectedRepo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("https://gitlab.com/owner/repo")]
    [InlineData("https://github.com/")]
    [InlineData("https://github.com/owner-only")]
    public void Parse_WithInvalidUrl_ThrowsArgumentException(string? url)
    {
        var act = () => GitHubUrlParser.Parse(url!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid GitHub URL*");
    }

    [Theory]
    [InlineData("https://github.com/owner/repo", "owner", "repo")]
    [InlineData("git+https://github.com/owner/repo.git", "owner", "repo")]
    [InlineData("git://github.com/owner/repo.git", "owner", "repo")]
    [InlineData("github:owner/repo", "owner", "repo")]
    [InlineData("owner/repo", "owner", "repo")]
    public void TryParse_WithValidUrl_ReturnsOwnerAndRepo(string url, string expectedOwner, string expectedRepo)
    {
        var (owner, repo) = GitHubUrlParser.TryParse(url);

        owner.Should().Be(expectedOwner);
        repo.Should().Be(expectedRepo);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("https://gitlab.com/owner/repo")]
    [InlineData("https://github.com/owner-only")]
    public void TryParse_WithInvalidUrl_ReturnsNulls(string url)
    {
        var (owner, repo) = GitHubUrlParser.TryParse(url);

        owner.Should().BeNull();
        repo.Should().BeNull();
    }
}
