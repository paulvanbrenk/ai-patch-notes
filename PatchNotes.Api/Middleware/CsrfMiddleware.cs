namespace PatchNotes.Api.Middleware;

public class CsrfMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _allowedOrigins;
    private readonly bool _isDevelopment;
    private readonly ILogger<CsrfMiddleware> _logger;

    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "HEAD", "OPTIONS"
    };

    public CsrfMiddleware(RequestDelegate next, IConfiguration configuration,
        IHostEnvironment environment, ILogger<CsrfMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _isDevelopment = environment.IsDevelopment();

        var origins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
        _allowedOrigins = new HashSet<string>(origins, StringComparer.OrdinalIgnoreCase);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (SafeMethods.Contains(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Skip CSRF validation for webhook endpoints (they use their own signature verification)
        if (context.Request.Path.StartsWithSegments("/webhooks"))
        {
            await _next(context);
            return;
        }

        var origin = context.Request.Headers.Origin.FirstOrDefault();

        // Browsers send Origin: "null" for cross-subdomain navigational form POSTs
        // (e.g. www.myreleasenotes.ai → api.myreleasenotes.ai). Fall back to the
        // Sec-Fetch-Site header, which is browser-enforced and cannot be spoofed.
        if (string.IsNullOrEmpty(origin) || origin == "null")
        {
            var fetchSite = context.Request.Headers["Sec-Fetch-Site"].FirstOrDefault();
            if (fetchSite is "same-origin" or "same-site")
            {
                await _next(context);
                return;
            }

            _logger.LogWarning("CSRF: Rejected {Method} {Path} - missing Origin header", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Forbidden: missing Origin header" });
            return;
        }

        var isAllowed = _allowedOrigins.Contains(origin)
            || (_isDevelopment && Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.Host == "localhost");

        if (!isAllowed)
        {
            _logger.LogWarning("CSRF: Rejected {Method} {Path} - disallowed Origin: {Origin}", context.Request.Method, context.Request.Path, origin);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Forbidden: origin not allowed" });
            return;
        }

        await _next(context);
    }
}
