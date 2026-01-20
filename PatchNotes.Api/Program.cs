using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PatchNotes.Data;
using PatchNotes.Data.GitHub;
using PatchNotes.Data.AI;
using PatchNotes.Sync;

var builder = WebApplication.CreateBuilder(args);

// API Key configuration - use environment variable in production
var apiKey = builder.Configuration["ApiKey"];
if (string.IsNullOrEmpty(apiKey))
{
    throw new InvalidOperationException("ApiKey configuration is required. Set it in appsettings.json or via APIKEY environment variable.");
}

builder.Services.AddOpenApi();

builder.Services.AddPatchNotesDbContext(builder.Configuration);

builder.Services.AddHttpClient();

builder.Services.AddGitHubClient(options =>
{
    options.Token = builder.Configuration["GitHub:Token"];
});

builder.Services.AddAiClient(options =>
{
    options.ApiKey = builder.Configuration["AI:ApiKey"];
    var baseUrl = builder.Configuration["AI:BaseUrl"];
    if (!string.IsNullOrEmpty(baseUrl))
    {
        options.BaseUrl = baseUrl;
    }
    var model = builder.Configuration["AI:Model"];
    if (!string.IsNullOrEmpty(model))
    {
        options.Model = model;
    }
});

builder.Services.AddScoped<SyncService>();

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

// API Key authentication filter for mutating endpoints
var requireApiKey = new Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>(
    (context, next) => async invocationContext =>
    {
        var httpContext = invocationContext.HttpContext;
        var requestApiKey = httpContext.Request.Headers["X-API-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(requestApiKey) || requestApiKey != apiKey)
        {
            return Results.Unauthorized();
        }

        return await next(invocationContext);
    });

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

// GET /api/packages/{id} - Get single package details
app.MapGet("/api/packages/{id:int}", async (int id, PatchNotesDbContext db) =>
{
    var package = await db.Packages
        .Where(p => p.Id == id)
        .Select(p => new
        {
            p.Id,
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
app.MapGet("/api/packages/{id:int}/releases", async (int id, PatchNotesDbContext db) =>
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

// GET /api/releases/{id} - Get single release details
app.MapGet("/api/releases/{id:int}", async (int id, PatchNotesDbContext db) =>
{
    var release = await db.Releases
        .Include(r => r.Package)
        .Where(r => r.Id == id)
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
        .FirstOrDefaultAsync();

    if (release == null)
    {
        return Results.NotFound(new { error = "Release not found" });
    }

    return Results.Ok(release);
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
}).AddEndpointFilterFactory(requireApiKey);

// PATCH /api/packages/{id} - Update package GitHub mapping
app.MapPatch("/api/packages/{id:int}", async (int id, UpdatePackageRequest request, PatchNotesDbContext db) =>
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
        package.NpmName,
        package.GithubOwner,
        package.GithubRepo,
        package.LastFetchedAt,
        package.CreatedAt
    });
}).AddEndpointFilterFactory(requireApiKey);

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
}).AddEndpointFilterFactory(requireApiKey);

// POST /api/packages/{id}/sync - Trigger sync for a specific package
app.MapPost("/api/packages/{id:int}/sync", async (int id, PatchNotesDbContext db, SyncService syncService) =>
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
        package.NpmName,
        package.LastFetchedAt,
        releasesAdded = result.ReleasesAdded
    });
}).AddEndpointFilterFactory(requireApiKey);

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

// POST /api/releases/{id}/summarize - Generate AI summary for a release
app.MapPost("/api/releases/{id:int}/summarize", async (int id, HttpContext httpContext, PatchNotesDbContext db, IAiClient aiClient) =>
{
    var release = await db.Releases
        .Include(r => r.Package)
        .FirstOrDefaultAsync(r => r.Id == id);

    if (release == null)
    {
        return Results.NotFound(new { error = "Release not found" });
    }

    var acceptHeader = httpContext.Request.Headers.Accept.ToString();

    // Support Server-Sent Events for streaming
    if (acceptHeader.Contains("text/event-stream"))
    {
        httpContext.Response.ContentType = "text/event-stream";
        httpContext.Response.Headers.CacheControl = "no-cache";
        httpContext.Response.Headers.Connection = "keep-alive";

        var fullSummary = new System.Text.StringBuilder();

        await foreach (var chunk in aiClient.SummarizeReleaseNotesStreamAsync(release.Title, release.Body, httpContext.RequestAborted))
        {
            fullSummary.Append(chunk);
            var chunkData = JsonSerializer.Serialize(new { type = "chunk", content = chunk });
            await httpContext.Response.WriteAsync($"data: {chunkData}\n\n", httpContext.RequestAborted);
            await httpContext.Response.Body.FlushAsync(httpContext.RequestAborted);
        }

        var completeData = JsonSerializer.Serialize(new
        {
            type = "complete",
            result = new
            {
                releaseId = release.Id,
                release.Tag,
                release.Title,
                summary = fullSummary.ToString(),
                package = new { release.Package.Id, release.Package.NpmName }
            }
        });
        await httpContext.Response.WriteAsync($"data: {completeData}\n\n", httpContext.RequestAborted);
        await httpContext.Response.WriteAsync("data: [DONE]\n\n", httpContext.RequestAborted);

        return Results.Empty;
    }

    // Non-streaming JSON response
    var summary = await aiClient.SummarizeReleaseNotesAsync(release.Title, release.Body);

    return Results.Ok(new
    {
        release.Id,
        release.Tag,
        release.Title,
        summary,
        Package = new
        {
            release.Package.Id,
            release.Package.NpmName
        }
    });
});

// GET /api/notifications - Query notifications
app.MapGet("/api/notifications", async (bool? unreadOnly, int? packageId, PatchNotesDbContext db) =>
{
    IQueryable<Notification> query = db.Notifications
        .Include(n => n.Package);

    if (unreadOnly == true)
    {
        query = query.Where(n => n.Unread);
    }

    if (packageId.HasValue)
    {
        query = query.Where(n => n.PackageId == packageId.Value);
    }

    var notifications = await query
        .OrderByDescending(n => n.UpdatedAt)
        .Select(n => new
        {
            n.Id,
            n.GitHubId,
            n.Reason,
            n.SubjectTitle,
            n.SubjectType,
            n.SubjectUrl,
            n.RepositoryFullName,
            n.Unread,
            n.UpdatedAt,
            n.LastReadAt,
            n.FetchedAt,
            Package = n.Package == null ? null : new
            {
                n.Package.Id,
                n.Package.NpmName,
                n.Package.GithubOwner,
                n.Package.GithubRepo
            }
        })
        .ToListAsync();

    return Results.Ok(notifications);
});

// GET /api/notifications/unread-count - Get count of unread notifications
app.MapGet("/api/notifications/unread-count", async (PatchNotesDbContext db) =>
{
    var count = await db.Notifications.CountAsync(n => n.Unread);
    return Results.Ok(new { count });
});

// PATCH /api/notifications/{id}/read - Mark notification as read
app.MapPatch("/api/notifications/{id:int}/read", async (int id, PatchNotesDbContext db) =>
{
    var notification = await db.Notifications.FindAsync(id);
    if (notification == null)
    {
        return Results.NotFound(new { error = "Notification not found" });
    }

    notification.Unread = false;
    notification.LastReadAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.Ok(new { notification.Id, notification.Unread, notification.LastReadAt });
}).AddEndpointFilterFactory(requireApiKey);

// DELETE /api/notifications/{id} - Delete a notification
app.MapDelete("/api/notifications/{id:int}", async (int id, PatchNotesDbContext db) =>
{
    var notification = await db.Notifications.FindAsync(id);
    if (notification == null)
    {
        return Results.NotFound(new { error = "Notification not found" });
    }

    db.Notifications.Remove(notification);
    await db.SaveChangesAsync();

    return Results.NoContent();
}).AddEndpointFilterFactory(requireApiKey);

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
record UpdatePackageRequest(string? GithubOwner, string? GithubRepo);

// Make the Program class accessible to the test project
public partial class Program { }
