using FluentAssertions;
using PatchNotes.Sync;

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
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("https://gitlab.com/owner/repo")]
    [InlineData("https://github.com/")]
    [InlineData("https://github.com/owner-only")]
    public void Parse_WithInvalidUrl_ThrowsArgumentException(string url)
    {
        var act = () => GitHubUrlParser.Parse(url);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid GitHub URL*");
    }
}
