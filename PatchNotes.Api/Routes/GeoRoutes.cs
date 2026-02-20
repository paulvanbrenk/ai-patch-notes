namespace PatchNotes.Api.Routes;

public static class GeoRoutes
{
    public static WebApplication MapGeoRoutes(this WebApplication app)
    {
        app.MapGet("/api/geo/country", (HttpContext httpContext) =>
        {
            // Cloudflare automatically sets CF-IPCountry on every request
            var countryCode = httpContext.Request.Headers["CF-IPCountry"].FirstOrDefault() ?? "XX";
            return Results.Ok(new { country_code = countryCode });
        })
        .ExcludeFromDescription();

        return app;
    }
}
