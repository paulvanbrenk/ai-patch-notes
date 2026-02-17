namespace PatchNotes.Sync.Core.AI.Models;

internal class ChatCompletionRequest
{
    public required string Model { get; set; }
    public required List<ChatMessage> Messages { get; set; }
    public int MaxTokens { get; set; }
    public double Temperature { get; set; }
    public bool Stream { get; set; }
}

internal class ChatMessage
{
    public required string Role { get; set; }
    public required string Content { get; set; }
}

internal class ChatCompletionResponse
{
    public List<Choice>? Choices { get; set; }
    public UsageInfo? Usage { get; set; }
}

internal class Choice
{
    public ChatMessage? Message { get; set; }
}

internal class UsageInfo
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

internal class ChatCompletionChunk
{
    public List<ChunkChoice>? Choices { get; set; }
}

internal class ChunkChoice
{
    public DeltaContent? Delta { get; set; }
}

internal class DeltaContent
{
    public string? Content { get; set; }
}
