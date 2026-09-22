using NickAI.Core.Chat;
using Xunit;

namespace NickAI.Core.Tests;

public sealed class MessageContentParserTests
{
    [Fact]
    public void Parse_returns_a_single_text_segment_for_prose()
    {
        var segments = MessageContentParser.Parse("Just a normal answer.");

        var segment = Assert.Single(segments);
        Assert.Equal(MessageSegmentKind.Text, segment.Kind);
        Assert.Equal("Just a normal answer.", segment.Text);
        Assert.Null(segment.Language);
    }

    [Fact]
    public void Parse_handles_empty_and_whitespace_content()
    {
        Assert.Empty(MessageContentParser.Parse(string.Empty));
        Assert.Empty(MessageContentParser.Parse("   \n\n  "));
    }

    [Fact]
    public void Parse_splits_prose_and_fenced_code()
    {
        const string content =
            "Here is the fix.\n\n" +
            "```csharp\n" +
            "var x = 1;\n" +
            "```\n\n" +
            "That should build.";

        var segments = MessageContentParser.Parse(content);

        Assert.Equal(3, segments.Count);
        Assert.Equal(MessageSegmentKind.Text, segments[0].Kind);
        Assert.Equal(MessageSegmentKind.Code, segments[1].Kind);
        Assert.Equal(MessageSegmentKind.Text, segments[2].Kind);

        Assert.Equal("csharp", segments[1].Language);
        Assert.Equal("var x = 1;", segments[1].Text);
        Assert.Equal("Here is the fix.", segments[0].Text);
        Assert.Equal("That should build.", segments[2].Text);
    }

    [Fact]
    public void Parse_supports_multiple_code_blocks()
    {
        const string content = "```cs\na\n```\ntext\n```py\nb\n```";

        var segments = MessageContentParser.Parse(content);

        Assert.Equal(3, segments.Count);
        Assert.Equal(MessageSegmentKind.Code, segments[0].Kind);
        Assert.Equal(MessageSegmentKind.Text, segments[1].Kind);
        Assert.Equal(MessageSegmentKind.Code, segments[2].Kind);
        Assert.Equal("py", segments[2].Language);
    }

    [Fact]
    public void Parse_reports_no_language_when_the_fence_has_no_tag()
    {
        var segments = MessageContentParser.Parse("```\nplain code\n```");

        var segment = Assert.Single(segments);
        Assert.Equal(MessageSegmentKind.Code, segment.Kind);
        Assert.Null(segment.Language);
        Assert.Equal("plain code", segment.Text);
    }

    [Fact]
    public void Parse_treats_an_unterminated_fence_as_code()
    {
        // Streaming responses arrive mid-fence constantly; a partial code block
        // must still render as code, not as prose.
        var segments = MessageContentParser.Parse("```python\nprint(\"partial");

        var segment = Assert.Single(segments);
        Assert.Equal(MessageSegmentKind.Code, segment.Kind);
        Assert.Equal("python", segment.Language);
        Assert.Equal("print(\"partial", segment.Text);
    }

    [Fact]
    public void Parse_normalizes_crlf_line_endings()
    {
        var segments = MessageContentParser.Parse("line one\r\n```cs\r\nvar x = 1;\r\n```\r\nline two");

        Assert.Equal(3, segments.Count);
        Assert.DoesNotContain("\r", segments[1].Text);
        Assert.Equal("var x = 1;", segments[1].Text);
        Assert.Equal("line one", segments[0].Text);
        Assert.Equal("line two", segments[2].Text);
    }

    [Fact]
    public void Parse_ignores_an_empty_code_block()
    {
        var segments = MessageContentParser.Parse("before\n```\n```\nafter");

        Assert.Equal(2, segments.Count);
        Assert.All(segments, s => Assert.Equal(MessageSegmentKind.Text, s.Kind));
    }

    [Fact]
    public void Parse_allows_an_indented_fence()
    {
        var segments = MessageContentParser.Parse("- item\n  ```cs\n  var y = 2;\n  ```");

        Assert.Equal(2, segments.Count);
        Assert.Equal(MessageSegmentKind.Code, segments[1].Kind);
        Assert.Equal("cs", segments[1].Language);
    }

    [Fact]
    public void FirstCodeBlock_returns_the_code_segment_or_null()
    {
        var code = MessageContentParser.FirstCodeBlock("prose\n```js\nlet a = 1;\n```");

        Assert.NotNull(code);
        Assert.Equal("js", code!.Language);
        Assert.Equal("let a = 1;", code.Text);

        Assert.Null(MessageContentParser.FirstCodeBlock("no code here"));
    }
}
