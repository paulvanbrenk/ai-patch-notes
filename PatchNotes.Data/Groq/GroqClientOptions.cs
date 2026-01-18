namespace PatchNotes.Data.Groq;

/// <summary>
/// Options for configuring the Groq client.
/// </summary>
public class GroqClientOptions
{
    /// <summary>
    /// The section name in configuration.
    /// </summary>
    public const string SectionName = "Groq";

    /// <summary>
    /// The Groq API base URL.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";

    /// <summary>
    /// The Groq API key for authentication.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The model to use for summarization.
    /// </summary>
    public string Model { get; set; } = "llama-3.3-70b-versatile";
}
