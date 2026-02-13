using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PatchNotes.Data;

namespace PatchNotes.Api.Routes;

public static class PackageRoutes
{
    public static WebApplication MapPackageRoutes(this WebApplication app)
    {
        var requireAuth = RouteUtils.CreateAuthFilter();
        var requireAdmin = RouteUtils.CreateAdminFilter();

        var group = app.MapGroup("/api/packages").WithTags("Packages");

        // GET /api/packages - List all tracked packages
        group.MapGet("/", async (PatchNotesDbContext db) =>
        {
            var packages = await db.Packages
                .Select(p => new PackageDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Url = p.Url,
                    NpmName = p.NpmName,
                    GithubOwner = p.GithubOwner,
                    GithubRepo = p.GithubRepo,
                    TagPrefix = p.TagPrefix,
                    LastFetchedAt = p.LastFetchedAt,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();
            return TypedResults.Ok(packages);
        })
        .Produces<List<PackageDto>>(StatusCodes.Status200OK)
        .WithName("GetPackages");

        // GET /api/packages/{id} - Get single package details
        group.MapGet("/{id}", async (string id, PatchNotesDbContext db) =>
        {
            var package = await db.Packages
                .Where(p => p.Id == id)
                .Select(p => new PackageDetailDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Url = p.Url,
                    NpmName = p.NpmName,
                    GithubOwner = p.GithubOwner,
                    GithubRepo = p.GithubRepo,
                    TagPrefix = p.TagPrefix,
                    LastFetchedAt = p.LastFetchedAt,
                    CreatedAt = p.CreatedAt,
                    ReleaseCount = p.Releases.Count
                })
                .FirstOrDefaultAsync();

            if (package == null)
            {
                return Results.NotFound(new { error = "Package not found" });
            }

            return Results.Ok(package);
        })
        .Produces<PackageDetailDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithName("GetPackage");

        // GET /api/packages/{id}/releases - Get all releases for a package
        group.MapGet("/{id}/releases", async (string id, PatchNotesDbContext db) =>
        {
            var packageExists = await db.Packages.AnyAsync(p => p.Id == id);
            if (!packageExists)
            {
                return Results.NotFound(new { error = "Package not found" });
            }

            var releases = await db.Releases
                .Where(r => r.PackageId == id)
                .OrderByDescending(r => r.PublishedAt)
                .Select(r => new PackageReleaseDto
                {
                    Id = r.Id,
                    Tag = r.Tag,
                    Title = r.Title,
                    Body = r.Body,
                    Summary = r.Summary,
                    SummaryGeneratedAt = r.SummaryGeneratedAt,
                    PublishedAt = r.PublishedAt,
                    FetchedAt = r.FetchedAt,
                    Package = new PackageReleasePackageDto
                    {
                        Id = r.Package.Id,
                        Name = r.Package.Name,
                        Url = r.Package.Url,
                        NpmName = r.Package.NpmName,
                        GithubOwner = r.Package.GithubOwner,
                        GithubRepo = r.Package.GithubRepo
                    }
                })
                .ToListAsync();

            return Results.Ok(releases);
        })
        .Produces<List<PackageReleaseDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithName("GetPackageReleases");

        // POST /api/packages - Add package to track
        group.MapPost("/", async (AddPackageRequest request, HttpContext httpContext, PatchNotesDbContext db, IHttpClientFactory httpClientFactory) =>
        {
            if (string.IsNullOrWhiteSpace(request.NpmName))
            {
                return Results.BadRequest(new { error = "npmName is required" });
            }

            // Check package limit for free users
            var stytchUserId = httpContext.Items["StytchUserId"] as string;
            if (!string.IsNullOrEmpty(stytchUserId))
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == stytchUserId);
                if (user != null)
                {
                    if (!user.IsPro)
                    {
                        var packageCount = await db.Watchlists.CountAsync(w => w.UserId == user.Id);
                        if (packageCount >= 5)
                        {
                            return Results.Json(new
                            {
                                error = "Package limit reached",
                                message = "Free accounts can track up to 5 packages. Upgrade to Pro for unlimited packages.",
                                limit = 5,
                                current = packageCount
                            }, statusCode: 403);
                        }
                    }
                }
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
                TagPrefix = request.TagPrefix,
            };

            db.Packages.Add(package);
            await db.SaveChangesAsync();

            return Results.Created($"/api/packages/{package.Id}", new PackageDto
            {
                Id = package.Id,
                Name = package.Name,
                Url = package.Url,
                NpmName = package.NpmName,
                GithubOwner = package.GithubOwner,
                GithubRepo = package.GithubRepo,
                TagPrefix = package.TagPrefix,
                CreatedAt = package.CreatedAt
            });
        })
        .AddEndpointFilterFactory(requireAuth)
        .AddEndpointFilterFactory(requireAdmin)
        .Produces<PackageDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .WithName("CreatePackage");

        // PATCH /api/packages/{id} - Update package GitHub mapping
        group.MapPatch("/{id}", async (string id, UpdatePackageRequest request, PatchNotesDbContext db) =>
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

            if (request.TagPrefix != null)
            {
                package.TagPrefix = request.TagPrefix == "" ? null : request.TagPrefix;
            }

            await db.SaveChangesAsync();

            return Results.Ok(new PackageDto
            {
                Id = package.Id,
                Name = package.Name,
                Url = package.Url,
                NpmName = package.NpmName,
                GithubOwner = package.GithubOwner,
                GithubRepo = package.GithubRepo,
                TagPrefix = package.TagPrefix,
                LastFetchedAt = package.LastFetchedAt,
                CreatedAt = package.CreatedAt
            });
        })
        .AddEndpointFilterFactory(requireAuth)
        .AddEndpointFilterFactory(requireAdmin)
        .Produces<PackageDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithName("UpdatePackage");

        // DELETE /api/packages/{id} - Remove package from tracking
        group.MapDelete("/{id}", async (string id, PatchNotesDbContext db) =>
        {
            var package = await db.Packages.FindAsync(id);
            if (package == null)
            {
                return Results.NotFound(new { error = "Package not found" });
            }

            db.Packages.Remove(package);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .AddEndpointFilterFactory(requireAuth)
        .AddEndpointFilterFactory(requireAdmin)
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .WithName("DeletePackage");

        return app;
    }
}

public class PackageDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? Url { get; set; }
    public string? NpmName { get; set; }
    public required string GithubOwner { get; set; }
    public required string GithubRepo { get; set; }
    public string? TagPrefix { get; set; }
    public DateTimeOffset? LastFetchedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class PackageDetailDto : PackageDto
{
    public int ReleaseCount { get; set; }
}

public class PackageReleaseDto
{
    public required string Id { get; set; }
    public required string Tag { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? Summary { get; set; }
    public DateTimeOffset? SummaryGeneratedAt { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public DateTimeOffset FetchedAt { get; set; }
    public required PackageReleasePackageDto Package { get; set; }
}

public class PackageReleasePackageDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? Url { get; set; }
    public string? NpmName { get; set; }
    public required string GithubOwner { get; set; }
    public required string GithubRepo { get; set; }
}
