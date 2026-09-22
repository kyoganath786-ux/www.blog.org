using NickAI.Core.Chat;

namespace NickAI.App.ViewModels;

public sealed class SegmentViewModel
{
    public SegmentViewModel(MessageSegment segment)
    {
        Kind = segment.Kind;
        Text = segment.Text;
        Language = segment.Language;
    }

    public MessageSegmentKind Kind { get; }

    public string Text { get; }

    public string? Language { get; }

    public bool IsCode => Kind == MessageSegmentKind.Code;

    public string LanguageLabel => string.IsNullOrWhiteSpace(Language) ? "code" : Language!;
}
