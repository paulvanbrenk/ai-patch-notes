using System.Data.Common;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PatchNotes.Data;
using PatchNotes.Data.Stytch;

namespace PatchNotes.Tests;

public class PatchNotesApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestApiKey = "test-api-key-12345";
    public const string TestSessionToken = "test-session-token-12345";
    public const string TestUserId = "test-user-id";

    private readonly MockNpmHandler _npmHandler = new();
    private SqliteConnection? _connection;

    public MockNpmHandler NpmHandler => _npmHandler;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PatchNotesDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Create a shared connection for in-memory SQLite
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            // Use in-memory SQLite for testing with shared connection
            services.AddDbContext<PatchNotesDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Remove existing HttpClientFactory and add mock
            services.RemoveAll<IHttpClientFactory>();
            services.AddSingleton<IHttpClientFactory>(new MockHttpClientFactory(_npmHandler));

            // Remove existing Stytch client and add mock
            services.RemoveAll<IStytchClient>();
            services.AddSingleton<IStytchClient>(new MockStytchClient(TestSessionToken, TestUserId));
        });

        builder.UseSetting("ApiKey", TestApiKey);
        builder.UseSetting("GitHub:Token", "test-github-token");
        builder.UseSetting("AI:ApiKey", "test-ai-key");
    }

    public async Task InitializeAsync()
    {
        // Force creation of the WebApplication and database
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public new Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var handler = new SessionCookieHandler(Server.CreateHandler(), TestSessionToken);
        var client = new HttpClient(handler)
        {
            BaseAddress = Server.BaseAddress
        };
        return client;
    }

    /// <summary>
    /// Handler that adds the Stytch session cookie to all requests.
    /// </summary>
    private class SessionCookieHandler : DelegatingHandler
    {
        private readonly string _sessionToken;

        public SessionCookieHandler(HttpMessageHandler inner, string sessionToken) : base(inner)
        {
            _sessionToken = sessionToken;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add("Cookie", $"stytch_session={_sessionToken}");
            return base.SendAsync(request, cancellationToken);
        }
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        _npmHandler.Clear();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}

public class MockHttpClientFactory : IHttpClientFactory
{
    private readonly MockNpmHandler _handler;

    public MockHttpClientFactory(MockNpmHandler handler)
    {
        _handler = handler;
    }

    public HttpClient CreateClient(string name)
    {
        return new HttpClient(_handler);
    }
}

public class MockNpmHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode Status, string Content)> _responses = new();

    public void SetupPackage(string packageName, string githubOwner, string githubRepo)
    {
        var content = $$"""
            {
                "name": "{{packageName}}",
                "repository": {
                    "type": "git",
                    "url": "git+https://github.com/{{githubOwner}}/{{githubRepo}}.git"
                }
            }
            """;
        _responses[$"https://registry.npmjs.org/{packageName}"] = (HttpStatusCode.OK, content);
    }

    public void SetupPackageNotFound(string packageName)
    {
        _responses[$"https://registry.npmjs.org/{packageName}"] = (HttpStatusCode.NotFound, """{"error": "Not found"}""");
    }

    public void SetupPackageWithoutRepo(string packageName)
    {
        var content = $$"""
            {
                "name": "{{packageName}}"
            }
            """;
        _responses[$"https://registry.npmjs.org/{packageName}"] = (HttpStatusCode.OK, content);
    }

    public void Clear()
    {
        _responses.Clear();
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri?.ToString() ?? "";

        if (_responses.TryGetValue(url, out var response))
        {
            return Task.FromResult(new HttpResponseMessage(response.Status)
            {
                Content = new StringContent(response.Content, System.Text.Encoding.UTF8, "application/json")
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("""{"error": "Not found"}""", System.Text.Encoding.UTF8, "application/json")
        });
    }
}

public class MockStytchClient : IStytchClient
{
    private readonly string _validSessionToken;
    private readonly string _userId;

    public MockStytchClient(string validSessionToken, string userId)
    {
        _validSessionToken = validSessionToken;
        _userId = userId;
    }

    public Task<StytchSessionResult?> AuthenticateSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        if (sessionToken == _validSessionToken)
        {
            return Task.FromResult<StytchSessionResult?>(new StytchSessionResult
            {
                UserId = _userId,
                SessionId = "test-session-id",
                Email = "test@example.com"
            });
        }

        return Task.FromResult<StytchSessionResult?>(null);
    }
}
