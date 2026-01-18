using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PatchNotes.Data.Groq;

/// <summary>
/// Client for interacting with the Groq API.
/// </summary>
public class GroqClient : IGroqClient
{
    private readonly HttpClient _httpClient;
    private readonly GroqClientOptions _options;
    private readonly ILogger<GroqClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GroqClient(
        HttpClient httpClient,
        IOptions<GroqClientOptions> options,
        ILogger<GroqClient> logger)
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

        var content = string.IsNullOrWhiteSpace(releaseTitle)
            ? releaseBody!
            : string.IsNullOrWhiteSpace(releaseBody)
                ? releaseTitle
                : $"# {releaseTitle}\n\n{releaseBody}";

        var request = new ChatCompletionRequest
        {
            Model = _options.Model,
            Messages =
            [
                new ChatMessage
                {
                    Role = "system",
                    Content = """
                        You are a technical writer that summarizes software release notes.
                        Provide a concise summary (2-4 sentences) that highlights:
                        - The most important new features or improvements
                        - Critical bug fixes
                        - Breaking changes (if any)
                        Keep the summary brief and focused on what matters most to developers.
                        Do not use markdown formatting in your response.
                        """
                },
                new ChatMessage
                {
                    Role = "user",
                    Content = $"Summarize these release notes:\n\n{content}"
                }
            ],
            MaxTokens = 256,
            Temperature = 0.3
        };

        _logger.LogDebug("Sending summarization request to Groq API using model {Model}", _options.Model);

        var response = await _httpClient.PostAsJsonAsync("chat/completions", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);

        if (result?.Choices is not { Count: > 0 })
        {
            _logger.LogWarning("Groq API returned empty response");
            return "Unable to generate summary.";
        }

        var summary = result.Choices[0].Message?.Content?.Trim() ?? "Unable to generate summary.";

        _logger.LogDebug(
            "Groq API response: {PromptTokens} prompt tokens, {CompletionTokens} completion tokens",
            result.Usage?.PromptTokens ?? 0,
            result.Usage?.CompletionTokens ?? 0);

        return summary;
    }

    private class ChatCompletionRequest
    {
        public required string Model { get; set; }
        public required List<ChatMessage> Messages { get; set; }
        public int MaxTokens { get; set; }
        public double Temperature { get; set; }
    }

    private class ChatMessage
    {
        public required string Role { get; set; }
        public required string Content { get; set; }
    }

    private class ChatCompletionResponse
    {
        public List<Choice>? Choices { get; set; }
        public UsageInfo? Usage { get; set; }
    }

    private class Choice
    {
        public ChatMessage? Message { get; set; }
    }

    private class UsageInfo
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
