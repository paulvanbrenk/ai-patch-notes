using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PatchNotes.Data.AI.Models;

namespace PatchNotes.Data.AI;

/// <summary>
/// Client for interacting with OpenAI-compatible APIs.
/// Supports multiple providers: OpenAI, Groq, OpenRouter, Ollama, etc.
/// </summary>
public class AiClient : IAiClient
{
    private readonly HttpClient _httpClient;
    private readonly AiClientOptions _options;
    private readonly ILogger<AiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly string SystemPrompt = LoadEmbeddedPrompt();

    public AiClient(
        HttpClient httpClient,
        IOptions<AiClientOptions> options,
        ILogger<AiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> SummarizeReleaseNotesAsync(
        string packageName,
        IReadOnlyList<ReleaseInput> releases,
        CancellationToken cancellationToken = default)
    {
        if (releases.Count == 0 || releases.All(r =>
                string.IsNullOrWhiteSpace(r.Body) && string.IsNullOrWhiteSpace(r.Title)))
        {
            return "No release notes content available to summarize.";
        }

        var userMessage = FormatUserMessage(packageName, releases);

        var request = new ChatCompletionRequest
        {
            Model = _options.Model,
            Messages =
            [
                new ChatMessage { Role = "system", Content = SystemPrompt },
                new ChatMessage { Role = "user", Content = userMessage }
            ],
            MaxTokens = 256,
            Temperature = 0.3,
            Stream = false
        };

        _logger.LogDebug("Sending summarization request to AI API using model {Model} for {ReleaseCount} release(s)",
            _options.Model, releases.Count);

        var response = await _httpClient.PostAsJsonAsync("chat/completions", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);

        if (result?.Choices is not { Count: > 0 })
        {
            _logger.LogWarning("AI API returned empty response");
            return "Unable to generate summary.";
        }

        var summary = result.Choices[0].Message?.Content?.Trim() ?? "Unable to generate summary.";

        _logger.LogDebug(
            "AI API response: {PromptTokens} prompt tokens, {CompletionTokens} completion tokens",
            result.Usage?.PromptTokens ?? 0,
            result.Usage?.CompletionTokens ?? 0);

        return summary;
    }

    public async IAsyncEnumerable<string> SummarizeReleaseNotesStreamAsync(
        string packageName,
        IReadOnlyList<ReleaseInput> releases,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (releases.Count == 0 || releases.All(r =>
                string.IsNullOrWhiteSpace(r.Body) && string.IsNullOrWhiteSpace(r.Title)))
        {
            yield return "No release notes content available to summarize.";
            yield break;
        }

        var userMessage = FormatUserMessage(packageName, releases);

        var request = new ChatCompletionRequest
        {
            Model = _options.Model,
            Messages =
            [
                new ChatMessage { Role = "system", Content = SystemPrompt },
                new ChatMessage { Role = "user", Content = userMessage }
            ],
            MaxTokens = 256,
            Temperature = 0.3,
            Stream = true
        };

        _logger.LogDebug("Sending streaming summarization request to AI API using model {Model}", _options.Model);

        var requestContent = new StringContent(
            JsonSerializer.Serialize(request, JsonOptions),
            Encoding.UTF8,
            "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = requestContent
        };

        var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(line)) continue;

            if (!line.StartsWith("data: ")) continue;

            var data = line[6..];
            if (data == "[DONE]") break;

            ChatCompletionChunk? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(data, JsonOptions);
            }
            catch (JsonException)
            {
                continue;
            }

            var deltaContent = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(deltaContent))
            {
                yield return deltaContent;
            }
        }
    }

    internal static string FormatUserMessage(string packageName, IReadOnlyList<ReleaseInput> releases)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Package: {packageName}");

        if (releases.Count == 1)
        {
            sb.AppendLine("Release:");
        }
        else
        {
            sb.AppendLine("Releases:");
        }

        foreach (var release in releases)
        {
            sb.AppendLine();
            var dateStr = release.PublishedAt.HasValue
                ? $" ({release.PublishedAt.Value:yyyy-MM-dd})"
                : "";
            sb.AppendLine($"--- {release.Tag}{dateStr} ---");

            if (!string.IsNullOrWhiteSpace(release.Title))
                sb.AppendLine(release.Title);

            if (!string.IsNullOrWhiteSpace(release.Body))
                sb.AppendLine(release.Body);
        }

        return sb.ToString().TrimEnd();
    }

    private static string LoadEmbeddedPrompt()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "PatchNotes.Data.AI.Prompts.changelog-summary.txt";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Could not find embedded prompt resource: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
