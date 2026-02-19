using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using PatchNotes.Data;

namespace PatchNotes.Api.Routes;

public static class SitemapRoutes
{
    private const string BaseUrl = "https://www.myreleasenotes.ai";

    public static WebApplication MapSitemapRoutes(this WebApplication app)
    {
        app.MapGet("/sitemap.xml", async (HttpContext httpContext, PatchNotesDbContext db) =>
        {
            httpContext.Response.Headers.CacheControl = "public, max-age=3600";
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

            var urls = new List<XElement>();

            // Static pages
            urls.Add(UrlElement(ns, "/", priority: "1.0", changefreq: "daily"));
            urls.Add(UrlElement(ns, "/about", priority: "0.5", changefreq: "monthly"));
            urls.Add(UrlElement(ns, "/pricing", priority: "0.5", changefreq: "monthly"));
            urls.Add(UrlElement(ns, "/privacy", priority: "0.3", changefreq: "monthly"));

            // Owner pages and package pages
            var packages = await db.Packages
                .AsNoTracking()
                .Select(p => new
                {
                    p.GithubOwner,
                    p.GithubRepo,
                    LatestReleaseDate = p.Releases
                        .Max(r => (DateTimeOffset?)r.PublishedAt),
                })
                .ToListAsync();

            var owners = packages
                .Select(p => p.GithubOwner)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var owner in owners)
            {
                urls.Add(UrlElement(ns, $"/packages/{owner}", priority: "0.6", changefreq: "weekly"));
            }

            foreach (var pkg in packages)
            {
                urls.Add(UrlElement(ns, $"/packages/{pkg.GithubOwner}/{pkg.GithubRepo}",
                    priority: "0.8", changefreq: "weekly", lastmod: pkg.LatestReleaseDate));
            }

            // Recent releases (last 1000 by PublishedAt)
            var releases = await db.Releases
                .AsNoTracking()
                .OrderByDescending(r => r.PublishedAt)
                .Take(1000)
                .Select(r => new { r.Id, r.PublishedAt })
                .ToListAsync();

            foreach (var release in releases)
            {
                urls.Add(UrlElement(ns, $"/releases/{release.Id}",
                    priority: "0.6", changefreq: "monthly", lastmod: release.PublishedAt));
            }

            var sitemap = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(ns + "urlset", urls));

            return Results.Text(sitemap.Declaration + "\n" + sitemap.ToString(), "application/xml");
        })
        .WithName("GetSitemap")
        .Produces<string>(StatusCodes.Status200OK, "application/xml")
        .ExcludeFromDescription();

        return app;
    }

    private static XElement UrlElement(XNamespace ns, string path,
        string priority, string changefreq, DateTimeOffset? lastmod = null)
    {
        var el = new XElement(ns + "url",
            new XElement(ns + "loc", BaseUrl + path),
            new XElement(ns + "changefreq", changefreq),
            new XElement(ns + "priority", priority));

        if (lastmod.HasValue)
        {
            el.Add(new XElement(ns + "lastmod", lastmod.Value.ToString("yyyy-MM-dd")));
        }

        return el;
    }
}
