using System.Text;

namespace NickAI.Core.Chat;

/// <summary>
/// Splits message content into prose and fenced code blocks so the UI can
/// render code differently and agents can extract generated code.
/// </summary>
public static class MessageContentParser
{
    public static IReadOnlyList<MessageSegment> Parse(string content)
    {
        var segments = new List<MessageSegment>();
        if (string.IsNullOrEmpty(content)) return segments;

        var lines = content.Replace("\r\n", "\n").Split('\n');
        var buffer = new StringBuilder();
        string? language = null;
        var inCode = false;

        void Flush()
        {
            if (buffer.Length == 0) return;
            var text = buffer.ToString().Trim('\n');
            if (string.IsNullOrWhiteSpace(text)) { buffer.Clear(); return; }
            segments.Add(inCode
                ? new MessageSegment(MessageSegmentKind.Code, text, language)
                : new MessageSegment(MessageSegmentKind.Text, text));
            buffer.Clear();
        }

        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                Flush();
                if (!inCode)
                {
                    inCode = true;
                    var tag = line.Trim().Substring(3).Trim();
                    language = tag.Length == 0 ? null : tag;
                }
                else
                {
                    inCode = false;
                    language = null;
                }
                continue;
            }

            buffer.Append(line).Append('\n');
        }

        // Unterminated code fence. This is the normal case while a response is
        // still streaming, so the partial block must keep rendering as code.
        if (buffer.Length > 0)
        {
            var text = buffer.ToString().Trim('\n');
            if (!string.IsNullOrWhiteSpace(text))
                segments.Add(inCode
                    ? new MessageSegment(MessageSegmentKind.Code, text, language)
                    : new MessageSegment(MessageSegmentKind.Text, text));
        }

        return segments;
    }

    /// <summary>Returns the first fenced code block, if any.</summary>
    public static MessageSegment? FirstCodeBlock(string content) =>
        Parse(content).FirstOrDefault(s => s.Kind == MessageSegmentKind.Code);
}
