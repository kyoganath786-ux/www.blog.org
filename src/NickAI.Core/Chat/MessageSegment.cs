namespace NickAI.Core.Chat;

public enum MessageSegmentKind
{
    Text,
    Code,
}

/// <summary>A contiguous piece of a message: prose or a fenced code block.</summary>
public sealed record MessageSegment(MessageSegmentKind Kind, string Text, string? Language = null);
