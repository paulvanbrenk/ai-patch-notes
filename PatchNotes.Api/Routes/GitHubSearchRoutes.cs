using PatchNotes.Sync.Core.GitHub;

namespace PatchNotes.Api.Routes;

public static class GitHubSearchRoutes
{
    public static WebApplication MapGitHubSearchRoutes(this WebApplication app)
    {
        var requireAuth = RouteUtils.CreateAuthFilter();

        var group = app.MapGroup("/api/github").WithTags("GitHubSearch");

        // GET /api/github/search?q={query} — search GitHub repositories (authenticated users)
        group.MapGet("/search", async (string? q, IGitHubClient gitHubClient) =>
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            {
                return Results.BadRequest(new ApiError("Query parameter 'q' is required and must be at least 2 characters"));
            }

            var results = await gitHubClient.SearchRepositoriesAsync(q.Trim(), perPage: 10);

            var dtos = results.Select(r => new GitHubRepoSearchResultDto
            {
                Owner = r.Owner.Login,
                Repo = r.Name,
                Description = r.Description,
                StarCount = r.StargazersCount,
            }).ToList();

            return Results.Ok(dtos);
        })
        .AddEndpointFilterFactory(requireAuth)
        .Produces<List<GitHubRepoSearchResultDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .WithName("SearchGitHubRepositoriesUser");

        return app;
    }
}
