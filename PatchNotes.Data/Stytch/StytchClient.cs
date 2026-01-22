using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PatchNotes.Data.Stytch;

/// <summary>
/// Client for interacting with the Stytch API.
/// </summary>
public class StytchClient : IStytchClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StytchClient> _logger;

    public StytchClient(HttpClient httpClient, ILogger<StytchClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<StytchSessionResult?> AuthenticateSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/v1/sessions/authenticate",
                new { session_token = sessionToken },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Stytch session authentication failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string? userId = null;
            string? sessionId = null;
            string? email = null;

            if (root.TryGetProperty("session", out var session))
            {
                if (session.TryGetProperty("user_id", out var userIdElement))
                {
                    userId = userIdElement.GetString();
                }
                if (session.TryGetProperty("session_id", out var sessionIdElement))
                {
                    sessionId = sessionIdElement.GetString();
                }
            }

            if (root.TryGetProperty("user", out var user))
            {
                if (user.TryGetProperty("emails", out var emails) &&
                    emails.ValueKind == JsonValueKind.Array &&
                    emails.GetArrayLength() > 0)
                {
                    var firstEmail = emails[0];
                    if (firstEmail.TryGetProperty("email", out var emailElement))
                    {
                        email = emailElement.GetString();
                    }
                }
            }

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(sessionId))
            {
                _logger.LogWarning("Stytch session response missing required fields");
                return null;
            }

            return new StytchSessionResult
            {
                UserId = userId,
                SessionId = sessionId,
                Email = email
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating Stytch session");
            return null;
        }
    }

    public async Task<StytchUser?> GetUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/v1/users/{userId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Stytch get user failed with status {StatusCode} for user {UserId}", response.StatusCode, userId);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string? email = null;
            string? name = null;
            string? status = null;

            // Extract email from emails array
            if (root.TryGetProperty("emails", out var emails) &&
                emails.ValueKind == JsonValueKind.Array &&
                emails.GetArrayLength() > 0)
            {
                var firstEmail = emails[0];
                if (firstEmail.TryGetProperty("email", out var emailElement))
                {
                    email = emailElement.GetString();
                }
            }

            // Extract name
            if (root.TryGetProperty("name", out var nameObj))
            {
                var firstName = nameObj.TryGetProperty("first_name", out var fn) ? fn.GetString() : null;
                var lastName = nameObj.TryGetProperty("last_name", out var ln) ? ln.GetString() : null;
                var nameParts = new[] { firstName, lastName }.Where(n => !string.IsNullOrEmpty(n));
                name = string.Join(" ", nameParts);
                if (string.IsNullOrEmpty(name)) name = null;
            }

            // Extract status
            if (root.TryGetProperty("status", out var statusElement))
            {
                status = statusElement.GetString();
            }

            return new StytchUser
            {
                UserId = userId,
                Email = email,
                Name = name,
                Status = status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Stytch user {UserId}", userId);
            return null;
        }
    }
}
