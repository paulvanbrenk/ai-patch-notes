using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PatchNotes.Data;
using PatchNotes.Api.Stytch;

namespace PatchNotes.Api.Routes;

public static class RouteUtils
{
    public static (string? owner, string? repo) ParseGitHubUrl(string url)
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
            var match = Regex.Match(url, pattern);
            if (match.Success)
            {
                var owner = match.Groups[1].Value;
                var repo = match.Groups[2].Value.Replace(".git", "");
                return (owner, repo);
            }
        }

        return (null, null);
    }

    public static Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate> CreateAuthFilter()
    {
        return (context, next) => async invocationContext =>
        {
            var httpContext = invocationContext.HttpContext;
            var stytchClient = httpContext.RequestServices.GetRequiredService<IStytchClient>();

            // Get session token from cookie (set by Stytch frontend SDK)
            var sessionToken = httpContext.Request.Cookies["stytch_session"];

            if (string.IsNullOrEmpty(sessionToken))
            {
                return Results.Unauthorized();
            }

            // Validate session with Stytch API
            var session = await stytchClient.AuthenticateSessionAsync(sessionToken, httpContext.RequestAborted);

            if (session == null)
            {
                return Results.Unauthorized();
            }

            // Store authenticated user info in HttpContext for use in endpoints
            httpContext.Items["StytchUserId"] = session.UserId;
            httpContext.Items["StytchSessionId"] = session.SessionId;
            httpContext.Items["StytchEmail"] = session.Email;
            httpContext.Items["StytchSession"] = session;

            return await next(invocationContext);
        };
    }

    /// <summary>
    /// Creates an endpoint filter that requires admin role (patch_notes_admin).
    /// Must be used after CreateAuthFilter().
    /// </summary>
    public static Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate> CreateAdminFilter()
    {
        return (context, next) => async invocationContext =>
        {
            var httpContext = invocationContext.HttpContext;

            // Get session from HttpContext (set by auth filter)
            var session = httpContext.Items["StytchSession"] as StytchSessionResult;

            if (session == null || !session.IsAdmin)
            {
                return Results.Json(new ApiError("Forbidden"), statusCode: StatusCodes.Status403Forbidden);
            }

            return await next(invocationContext);
        };
    }

    /// <summary>
    /// Attempts to authenticate the user and return their watchlist package IDs.
    /// Returns null if the user is not authenticated; returns an empty list if authenticated but no watchlist.
    /// </summary>
    public static async Task<List<string>?> GetAuthenticatedUserWatchlistIds(
        HttpContext httpContext, PatchNotesDbContext db, IStytchClient stytchClient)
    {
        var sessionToken = httpContext.Request.Cookies["stytch_session"];
        if (string.IsNullOrEmpty(sessionToken))
            return null;

        var session = await stytchClient.AuthenticateSessionAsync(sessionToken, httpContext.RequestAborted);
        if (session == null)
            return null;

        var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == session.UserId);
        if (user == null)
            return null;

        return await db.Watchlists
            .Where(w => w.UserId == user.Id)
            .Select(w => w.PackageId)
            .ToListAsync();
    }

    /// <summary>
    /// Resolves package IDs to filter by: user's watchlist if authenticated with a non-empty
    /// watchlist, otherwise the default watchlist from config.
    /// Returns (packageIds, hasWatchlistConfig) where hasWatchlistConfig indicates whether
    /// any watchlist source was available (user watchlist or default config).
    /// </summary>
    public static async Task<(List<string> ids, bool hasWatchlistConfig)> ResolveWatchlistPackageIds(
        HttpContext httpContext, PatchNotesDbContext db,
        IStytchClient stytchClient, DefaultWatchlistOptions defaultWatchlist)
    {
        var userWatchlistIds = await GetAuthenticatedUserWatchlistIds(httpContext, db, stytchClient);
        if (userWatchlistIds is { Count: > 0 })
        {
            return (userWatchlistIds, true);
        }

        // Fall back to default watchlist
        if (defaultWatchlist.Packages.Length == 0)
        {
            return ([], false);
        }

        var ownerRepoPairs = defaultWatchlist.Packages
            .Select(p => p.Split('/'))
            .Where(parts => parts.Length == 2)
            .Select(parts => parts[0] + "/" + parts[1])
            .ToList();

        var ids = await db.Packages
            .Where(p => ownerRepoPairs.Contains(p.GithubOwner + "/" + p.GithubRepo))
            .Select(p => p.Id)
            .ToListAsync();

        return (ids, true);
    }
}

public record ApiError(string Error, string? Details = null);

public record AddPackageRequest(string NpmName, string? TagPrefix = null);
public record UpdatePackageRequest(string? GithubOwner, string? GithubRepo, string? TagPrefix = null);
