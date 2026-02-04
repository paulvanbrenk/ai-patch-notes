using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PatchNotes.Data;
using PatchNotes.Sync;

namespace PatchNotes.Api.Routes;

public static class PackageRoutes
{
    public static WebApplication MapPackageRoutes(this WebApplication app)
    {
        var requireAuth = RouteUtils.CreateAuthFilter();
        var requireAdmin = RouteUtils.CreateAdminFilter();

        // GET /api/packages - List all tracked packages
        app.MapGet("/api/packages", async (PatchNotesDbContext db) =>
        {
            var packages = await db.Packages
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Url,
                    p.NpmName,
                    p.GithubOwner,
                    p.GithubRepo,
                    p.LastFetchedAt,
                    p.CreatedAt
                })
                .ToListAsync();
            return Results.Ok(packages);
        });

        // GET /api/packages/{id} - Get single package details
        app.MapGet("/api/packages/{id}", async (string id, PatchNotesDbContext db) =>
        {
            var package = await db.Packages
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Url,
                    p.NpmName,
                    p.GithubOwner,
                    p.GithubRepo,
                    p.LastFetchedAt,
                    p.CreatedAt,
                    ReleaseCount = p.Releases.Count
                })
                .FirstOrDefaultAsync();

            if (package == null)
            {
                return Results.NotFound(new { error = "Package not found" });
            }

            return Results.Ok(package);
        });

        // GET /api/packages/{id}/releases - Get all releases for a package
        app.MapGet("/api/packages/{id}/releases", async (string id, PatchNotesDbContext db) =>
        {
            var packageExists = await db.Packages.AnyAsync(p => p.Id == id);
            if (!packageExists)
            {
                return Results.NotFound(new { error = "Package not found" });
            }

            var releases = await db.Releases
                .Where(r => r.PackageId == id)
                .OrderByDescending(r => r.PublishedAt)
                .Select(r => new
                {
                    r.Id,
                    r.Version,
                    r.Title,
                    r.Body,
                    r.PublishedAt,
                    r.FetchedAt,
                    r.Major,
                    r.Minor,
                    r.IsPrerelease,
                    Package = new
                    {
                        r.Package.Id,
                        r.Package.Name,
                        r.Package.Url,
                        r.Package.NpmName,
                        r.Package.GithubOwner,
                        r.Package.GithubRepo
                    }
                })
                .ToListAsync();

            return Results.Ok(releases);
        });

        // POST /api/packages - Add package to track
        app.MapPost("/api/packages", async (AddPackageRequest request, PatchNotesDbContext db, IHttpClientFactory httpClientFactory) =>
        {
            if (string.IsNullOrWhiteSpace(request.NpmName))
            {
                return Results.BadRequest(new { error = "npmName is required" });
            }

            var existing = await db.Packages.FirstOrDefaultAsync(p => p.NpmName == request.NpmName);
            if (existing != null)
            {
                return Results.Conflict(new { error = "Package already exists", package = new { existing.Id, existing.NpmName } });
            }

            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", "PatchNotes");

            var npmUrl = $"https://registry.npmjs.org/{request.NpmName}";
            HttpResponseMessage npmResponse;
            try
            {
                npmResponse = await client.GetAsync(npmUrl);
            }
            catch
            {
                return Results.BadRequest(new { error = "Failed to fetch package from npm registry" });
            }

            if (!npmResponse.IsSuccessStatusCode)
            {
                return Results.NotFound(new { error = "Package not found on npm" });
            }

            var npmJson = await npmResponse.Content.ReadAsStringAsync();
            var npmData = JsonDocument.Parse(npmJson);

            string? repoUrl = null;
            if (npmData.RootElement.TryGetProperty("repository", out var repo))
            {
                if (repo.ValueKind == JsonValueKind.String)
                {
                    repoUrl = repo.GetString();
                }
                else if (repo.TryGetProperty("url", out var urlProp))
                {
                    repoUrl = urlProp.GetString();
                }
            }

            if (string.IsNullOrEmpty(repoUrl))
            {
                return Results.BadRequest(new { error = "Package does not have a GitHub repository" });
            }

            // Parse GitHub owner/repo from URL
            var (owner, repoName) = RouteUtils.ParseGitHubUrl(repoUrl);
            if (owner == null || repoName == null)
            {
                return Results.BadRequest(new { error = "Could not parse GitHub repository URL", repositoryUrl = repoUrl });
            }

            var package = new Package
            {
                Name = request.NpmName,
                Url = $"https://github.com/{owner}/{repoName}",
                NpmName = request.NpmName,
                GithubOwner = owner,
                GithubRepo = repoName,
                CreatedAt = DateTime.UtcNow
            };

            db.Packages.Add(package);
            await db.SaveChangesAsync();

            return Results.Created($"/api/packages/{package.Id}", new
            {
                package.Id,
                package.Name,
                package.Url,
                package.NpmName,
                package.GithubOwner,
                package.GithubRepo,
                package.CreatedAt
            });
        }).AddEndpointFilterFactory(requireAuth)
          .AddEndpointFilterFactory(requireAdmin);

        // PATCH /api/packages/{id} - Update package GitHub mapping
        app.MapPatch("/api/packages/{id}", async (string id, UpdatePackageRequest request, PatchNotesDbContext db) =>
        {
            var package = await db.Packages.FindAsync(id);
            if (package == null)
            {
                return Results.NotFound(new { error = "Package not found" });
            }

            if (!string.IsNullOrWhiteSpace(request.GithubOwner))
            {
                package.GithubOwner = request.GithubOwner;
            }

            if (!string.IsNullOrWhiteSpace(request.GithubRepo))
            {
                package.GithubRepo = request.GithubRepo;
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                package.Id,
                package.Name,
                package.Url,
                package.NpmName,
                package.GithubOwner,
                package.GithubRepo,
                package.LastFetchedAt,
                package.CreatedAt
            });
        }).AddEndpointFilterFactory(requireAuth)
          .AddEndpointFilterFactory(requireAdmin);

        // DELETE /api/packages/{id} - Remove package from tracking
        app.MapDelete("/api/packages/{id}", async (string id, PatchNotesDbContext db) =>
        {
            var package = await db.Packages.FindAsync(id);
            if (package == null)
            {
                return Results.NotFound(new { error = "Package not found" });
            }

            db.Packages.Remove(package);
            await db.SaveChangesAsync();

            return Results.NoContent();
        }).AddEndpointFilterFactory(requireAuth)
          .AddEndpointFilterFactory(requireAdmin);

        // POST /api/packages/{id}/sync - Trigger sync for a specific package
        app.MapPost("/api/packages/{id}/sync", async (string id, PatchNotesDbContext db, SyncService syncService) =>
        {
            var package = await db.Packages.FindAsync(id);
            if (package == null)
            {
                return Results.NotFound(new { error = "Package not found" });
            }

            var result = await syncService.SyncPackageAsync(package);

            return Results.Ok(new
            {
                package.Id,
                package.Name,
                package.NpmName,
                package.LastFetchedAt,
                releasesAdded = result.ReleasesAdded
            });
        }).AddEndpointFilterFactory(requireAuth)
          .AddEndpointFilterFactory(requireAdmin);

        return app;
    }
}
