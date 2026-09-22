namespace NickAI.Core.Models;

/// <summary>Who produced a message in a conversation.</summary>
public enum ChatRole
{
    System,
    User,
    Assistant,
    Tool,
}

/// <summary>A single message in a conversation.</summary>
public sealed class ChatMessage
{
    public string Id { get; init; } = Guid.NewGuid().ToString("n");

    public ChatRole Role { get; init; }

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;

    /// <summary>Artifact ids produced or referenced by this message.</summary>
    public List<string> ArtifactIds { get; } = new();

    /// <summary>Human-readable activity lines ("Planning...", "Building..."). Never chain-of-thought.</summary>
    public List<string> Activity { get; } = new();

    /// <summary>Set for tool messages: which tool/agent produced this.</summary>
    public string? ToolName { get; init; }

    /// <summary>True while the assistant message is still streaming.</summary>
    public bool IsStreaming { get; set; }

    /// <summary>Populated when generation failed, so the UI can show a real error.</summary>
    public string? Error { get; set; }
}
