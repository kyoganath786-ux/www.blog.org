namespace NickAI.Core.Models;

/// <summary>An ordered set of messages, optionally attached to a project.</summary>
public sealed class Conversation
{
    public string Id { get; init; } = Guid.NewGuid().ToString("n");

    public string Title { get; set; } = "New chat";

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

    public string? ProjectId { get; set; }

    public List<ChatMessage> Messages { get; } = new();

    /// <summary>Ollama model name used for this conversation.</summary>
    public string? Model { get; set; }
}
