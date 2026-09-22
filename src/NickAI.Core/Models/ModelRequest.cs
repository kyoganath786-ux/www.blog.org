namespace NickAI.Core.Models;

/// <summary>A request sent to a model provider.</summary>
public sealed class ModelRequest
{
    public required string Model { get; init; }

    public required IReadOnlyList<ChatMessage> Messages { get; init; }

    public string? SystemPrompt { get; init; }

    public double Temperature { get; init; } = 0.7;

    /// <summary>Context window in tokens; 0 means provider default.</summary>
    public int ContextSize { get; init; }

    public int MaxTokens { get; init; } = -1;
}

/// <summary>Result of a non-streaming completion.</summary>
public sealed record ModelResult(string Text, string Model, TimeSpan Duration, int? PromptTokens = null, int? CompletionTokens = null);
