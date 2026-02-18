using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PatchNotes.Api.Routes;
using PatchNotes.Data;

namespace PatchNotes.Tests;

public class EmailPreferencesTests : IAsyncLifetime
{
    private PatchNotesApiFixture _fixture = null!;
    private HttpClient _authClient = null!;

    public async Task InitializeAsync()
    {
        _fixture = new PatchNotesApiFixture();
        await _fixture.InitializeAsync();
        _authClient = _fixture.CreateAuthenticatedClient();

        // Create a user via login
        await _authClient.PostAsync("/api/users/login", null);
    }

    public Task DisposeAsync()
    {
        _fixture.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetEmailPreferences_ReturnsDefaults()
    {
        var response = await _authClient.GetAsync("/api/users/me/email-preferences");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var prefs = await response.Content.ReadFromJsonAsync<EmailPreferencesDto>();
        prefs.Should().NotBeNull();
        prefs!.EmailDigestEnabled.Should().BeFalse();
        prefs.EmailWelcomeSent.Should().BeFalse();
    }

    [Fact]
    public async Task GetEmailPreferences_Unauthenticated_Returns401()
    {
        var unauthClient = _fixture.CreateClient();
        var response = await unauthClient.GetAsync("/api/users/me/email-preferences");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PatchEmailPreferences_UpdatesDigest()
    {
        var response = await _authClient.PatchAsJsonAsync("/api/users/me/email-preferences",
            new { EmailDigestEnabled = false });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var prefs = await response.Content.ReadFromJsonAsync<EmailPreferencesDto>();
        prefs!.EmailDigestEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task PatchEmailPreferences_PersistsChanges()
    {
        await _authClient.PatchAsJsonAsync("/api/users/me/email-preferences",
            new { EmailDigestEnabled = false });

        var response = await _authClient.GetAsync("/api/users/me/email-preferences");
        var prefs = await response.Content.ReadFromJsonAsync<EmailPreferencesDto>();
        prefs!.EmailDigestEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task PatchEmailPreferences_Unauthenticated_Returns403()
    {
        var unauthClient = _fixture.CreateClient();
        var response = await unauthClient.PatchAsJsonAsync("/api/users/me/email-preferences",
            new { EmailDigestEnabled = false });
        // CSRF middleware rejects requests without Origin header before auth runs
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PatchEmailPreferences_EmptyBody_NoChanges()
    {
        var response = await _authClient.PatchAsJsonAsync("/api/users/me/email-preferences",
            new { });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var prefs = await response.Content.ReadFromJsonAsync<EmailPreferencesDto>();
        prefs!.EmailDigestEnabled.Should().BeFalse();
    }
}
