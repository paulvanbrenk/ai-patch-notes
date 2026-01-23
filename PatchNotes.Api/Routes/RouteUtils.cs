using System.Text.RegularExpressions;
using PatchNotes.Data.Stytch;

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

    /// <summary>
    /// Determines if a tag represents a pre-release version.
    /// Delegates to VersionParser for comprehensive detection.
    /// </summary>
    public static bool IsPrerelease(string tag) => VersionParser.IsPrerelease(tag);

    /// <summary>
    /// Extracts the major version from a tag.
    /// Delegates to VersionParser for comprehensive format support.
    /// </summary>
    public static int? GetMajorVersion(string tag) => VersionParser.GetMajorVersion(tag);

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
                return Results.Forbid();
            }

            return await next(invocationContext);
        };
    }
}

public record AddPackageRequest(string NpmName);
public record UpdatePackageRequest(string? GithubOwner, string? GithubRepo);
