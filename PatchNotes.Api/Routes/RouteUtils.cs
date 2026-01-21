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

    public static bool IsPrerelease(string tag)
    {
        var lowerTag = tag.ToLowerInvariant();
        return lowerTag.Contains("alpha") ||
               lowerTag.Contains("beta") ||
               lowerTag.Contains("canary") ||
               lowerTag.Contains("preview") ||
               lowerTag.Contains("rc") ||
               lowerTag.Contains("next") ||
               lowerTag.Contains("nightly") ||
               lowerTag.Contains("dev") ||
               lowerTag.Contains("experimental");
    }

    public static int? GetMajorVersion(string tag)
    {
        // Extract major version from tags like "v15.0.0", "15.0.0", "v15.0.0-rc.1"
        var match = Regex.Match(tag, @"v?(\d+)\.");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var major))
        {
            return major;
        }
        return null;
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

            return await next(invocationContext);
        };
    }
}

public record AddPackageRequest(string NpmName);
public record UpdatePackageRequest(string? GithubOwner, string? GithubRepo);
public record UserLoginRequest(string StytchUserId, string? Email, string? Name);
