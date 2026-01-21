using System.Net.Http.Json;
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

    private const string SystemPrompt = """
        You are a technical writer that summarizes software release notes.
        Provide a concise summary (2-4 sentences) that highlights:
        - The most important new features or improvements
        - Critical bug fixes
        - Breaking changes (if any)
        Keep the summary brief and focused on what matters most to developers.
        Do not use markdown formatting in your response.
        """;

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
        string? releaseTitle,
        string? releaseBody,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(releaseBody) && string.IsNullOrWhiteSpace(releaseTitle))
        {
            return "No release notes content available to summarize.";
        }

        var content = FormatContent(releaseTitle, releaseBody);

        var request = new ChatCompletionRequest
        {
            Model = _options.Model,
            Messages =
            [
                new ChatMessage { Role = "system", Content = SystemPrompt },
                new ChatMessage { Role = "user", Content = $"Summarize these release notes:\n\n{content}" }
            ],
            MaxTokens = 256,
            Temperature = 0.3,
            Stream = false
        };

        _logger.LogDebug("Sending summarization request to AI API using model {Model}", _options.Model);

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
        string? releaseTitle,
        string? releaseBody,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(releaseBody) && string.IsNullOrWhiteSpace(releaseTitle))
        {
            yield return "No release notes content available to summarize.";
            yield break;
        }

        var content = FormatContent(releaseTitle, releaseBody);

        var request = new ChatCompletionRequest
        {
            Model = _options.Model,
            Messages =
            [
                new ChatMessage { Role = "system", Content = SystemPrompt },
                new ChatMessage { Role = "user", Content = $"Summarize these release notes:\n\n{content}" }
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

    private static string FormatContent(string? releaseTitle, string? releaseBody)
    {
        return string.IsNullOrWhiteSpace(releaseTitle)
            ? releaseBody!
            : string.IsNullOrWhiteSpace(releaseBody)
                ? releaseTitle
                : $"# {releaseTitle}\n\n{releaseBody}";
    }
}
