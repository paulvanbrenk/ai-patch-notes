using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PatchNotes.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<PatchNotesDbContext>(options =>
    options.UseSqlite("Data Source=../PatchNotes.Data/patchnotes.db"));

builder.Services.AddHttpClient();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

// GET /api/packages - List all tracked packages
app.MapGet("/api/packages", async (PatchNotesDbContext db) =>
{
    var packages = await db.Packages
        .Select(p => new
        {
            p.Id,
            p.NpmName,
            p.GithubOwner,
            p.GithubRepo,
            p.LastFetchedAt,
            p.CreatedAt
        })
        .ToListAsync();
    return Results.Ok(packages);
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
    // Formats: git+https://github.com/owner/repo.git, https://github.com/owner/repo, github:owner/repo
    var (owner, repoName) = ParseGitHubUrl(repoUrl);
    if (owner == null || repoName == null)
    {
        return Results.BadRequest(new { error = "Could not parse GitHub repository URL", repositoryUrl = repoUrl });
    }

    var package = new Package
    {
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
        package.NpmName,
        package.GithubOwner,
        package.GithubRepo,
        package.CreatedAt
    });
});

// DELETE /api/packages/{id} - Remove package from tracking
app.MapDelete("/api/packages/{id:int}", async (int id, PatchNotesDbContext db) =>
{
    var package = await db.Packages.FindAsync(id);
    if (package == null)
    {
        return Results.NotFound(new { error = "Package not found" });
    }

    db.Packages.Remove(package);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

// GET /api/releases - Query releases for selected packages
app.MapGet("/api/releases", async (string? packages, int? days, PatchNotesDbContext db) =>
{
    var daysToQuery = days ?? 7;
    var cutoffDate = DateTime.UtcNow.AddDays(-daysToQuery);

    IQueryable<Release> query = db.Releases
        .Include(r => r.Package)
        .Where(r => r.PublishedAt >= cutoffDate);

    if (!string.IsNullOrEmpty(packages))
    {
        var packageIds = packages.Split(',')
            .Select(p => int.TryParse(p.Trim(), out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        if (packageIds.Count > 0)
        {
            query = query.Where(r => packageIds.Contains(r.PackageId));
        }
    }

    var releases = await query
        .OrderByDescending(r => r.PublishedAt)
        .Select(r => new
        {
            r.Id,
            r.Tag,
            r.Title,
            r.Body,
            r.PublishedAt,
            r.FetchedAt,
            Package = new
            {
                r.Package.Id,
                r.Package.NpmName,
                r.Package.GithubOwner,
                r.Package.GithubRepo
            }
        })
        .ToListAsync();

    return Results.Ok(releases);
});

app.Run();

static (string? owner, string? repo) ParseGitHubUrl(string url)
{
    // Handle various formats:
    // git+https://github.com/owner/repo.git
    // https://github.com/owner/repo.git
    // https://github.com/owner/repo
    // git://github.com/owner/repo.git
    // github:owner/repo

    url = url.Trim();

    // Handle github:owner/repo shorthand
    if (url.StartsWith("github:"))
    {
        var parts = url[7..].Split('/');
        if (parts.Length >= 2)
        {
            return (parts[0], parts[1].Replace(".git", ""));
        }
    }

    // Handle URL formats
    var patterns = new[]
    {
        @"github\.com[:/]([^/]+)/([^/\.]+)",
    };

    foreach (var pattern in patterns)
    {
        var match = System.Text.RegularExpressions.Regex.Match(url, pattern);
        if (match.Success)
        {
            var owner = match.Groups[1].Value;
            var repo = match.Groups[2].Value.Replace(".git", "");
            return (owner, repo);
        }
    }

    return (null, null);
}

record AddPackageRequest(string NpmName);
