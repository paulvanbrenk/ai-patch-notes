using System.Net;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PatchNotes.Data;

namespace PatchNotes.Tests;

public class SitemapApiTests : IAsyncLifetime
{
    private PatchNotesApiFixture _fixture = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _fixture = new PatchNotesApiFixture();
        await _fixture.InitializeAsync();
        _client = _fixture.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _fixture.DisposeAsync();
        _fixture.Dispose();
    }

    [Fact]
    public async Task GetSitemap_GivenRequest_ReturnsXmlContentType()
    {
        var response = await _client.GetAsync("/sitemap.xml");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/xml");
    }

    [Fact]
    public async Task GetSitemap_GivenRequest_ContainsStaticPageUrls()
    {
        var response = await _client.GetAsync("/sitemap.xml");
        var xml = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(xml);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        var locs = doc.Descendants(ns + "loc").Select(e => e.Value).ToList();

        locs.Should().Contain("https://www.myreleasenotes.ai/");
        locs.Should().Contain("https://www.myreleasenotes.ai/about");
        locs.Should().Contain("https://www.myreleasenotes.ai/pricing");
        locs.Should().Contain("https://www.myreleasenotes.ai/privacy");
    }

    [Fact]
    public async Task GetSitemap_GivenPackagesAndReleasesExist_IncludesTheirUrls()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var pkg = new Package
        {
            Name = "react",
            Url = "https://github.com/facebook/react",
            NpmName = "react",
            GithubOwner = "facebook",
            GithubRepo = "react"
        };
        db.Packages.Add(pkg);

        var release = new Release
        {
            PackageId = pkg.Id,
            Tag = "v19.0.0",
            Title = "React 19",
            PublishedAt = new DateTimeOffset(2025, 3, 15, 0, 0, 0, TimeSpan.Zero),
            FetchedAt = DateTimeOffset.UtcNow
        };
        db.Releases.Add(release);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/sitemap.xml");
        var xml = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(xml);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        var urls = doc.Descendants(ns + "url").ToList();
        var locs = urls.Select(u => u.Element(ns + "loc")!.Value).ToList();

        // Assert - owner page
        locs.Should().Contain("https://www.myreleasenotes.ai/packages/facebook");

        // Assert - package page with lastmod
        var packageUrl = urls.First(u => u.Element(ns + "loc")!.Value == "https://www.myreleasenotes.ai/packages/facebook/react");
        packageUrl.Element(ns + "lastmod")!.Value.Should().Be("2025-03-15");
        packageUrl.Element(ns + "priority")!.Value.Should().Be("0.8");

        // Assert - release page with lastmod
        var releaseUrl = urls.First(u => u.Element(ns + "loc")!.Value == $"https://www.myreleasenotes.ai/releases/{release.Id}");
        releaseUrl.Element(ns + "lastmod")!.Value.Should().Be("2025-03-15");
        releaseUrl.Element(ns + "priority")!.Value.Should().Be("0.6");
    }

    [Fact]
    public async Task GetSitemap_GivenRequest_SetsCacheControlHeader()
    {
        var response = await _client.GetAsync("/sitemap.xml");

        response.Headers.CacheControl!.Public.Should().BeTrue();
        response.Headers.CacheControl!.MaxAge.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task GetSitemap_GivenMoreThan1000Releases_LimitsTo1000()
    {
        // Arrange - create a package with 1010 releases
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var pkg = new Package
        {
            Name = "prolific",
            Url = "https://github.com/test/prolific",
            NpmName = "prolific",
            GithubOwner = "test",
            GithubRepo = "prolific"
        };
        db.Packages.Add(pkg);

        for (var i = 0; i < 1010; i++)
        {
            db.Releases.Add(new Release
            {
                PackageId = pkg.Id,
                Tag = $"v1.0.{i}",
                Title = $"Release {i}",
                PublishedAt = DateTimeOffset.UtcNow.AddHours(-i),
                FetchedAt = DateTimeOffset.UtcNow
            });
        }
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/sitemap.xml");
        var xml = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(xml);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        var releaseLocs = doc.Descendants(ns + "loc")
            .Select(e => e.Value)
            .Where(loc => loc.Contains("/releases/"))
            .ToList();

        // Assert - should be capped at 1000 release URLs
        releaseLocs.Should().HaveCount(1000);
    }
}
