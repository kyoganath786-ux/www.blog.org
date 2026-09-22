using System.Collections.ObjectModel;
using NickAI.Core.Chat;
using NickAI.Core.Models;

namespace NickAI.App.ViewModels;

public sealed class MessageViewModel : ObservableObject
{
    private string _text;
    private bool _isStreaming;
    private string? _error;
    private string? _planSummary;

    public MessageViewModel(ChatRole role, string text)
    {
        Role = role;
        _text = text;
        RefreshSegments();
    }

    public ChatRole Role { get; }

    public bool IsUser => Role == ChatRole.User;

    public bool IsAssistant => Role == ChatRole.Assistant;

    public string RoleLabel => IsUser ? "You" : "NICK AI";

    public string Text
    {
        get => _text;
        set
        {
            if (SetProperty(ref _text, value))
                RefreshSegments();
        }
    }

    public bool IsStreaming
    {
        get => _isStreaming;
        set
        {
            if (SetProperty(ref _isStreaming, value))
                OnPropertyChanged(nameof(ShowPlainText));
        }
    }

    /// <summary>While streaming we render plain text; when finished we render parsed segments.</summary>
    public bool ShowPlainText => _isStreaming;

    public bool ShowSegments => !_isStreaming;

    public string? Error
    {
        get => _error;
        set
        {
            if (SetProperty(ref _error, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(_error);

    public string? PlanSummary
    {
        get => _planSummary;
        set
        {
            if (SetProperty(ref _planSummary, value))
                OnPropertyChanged(nameof(HasPlan));
        }
    }

    public bool HasPlan => !string.IsNullOrWhiteSpace(_planSummary);

    public ObservableCollection<string> Activity { get; } = new();

    public ObservableCollection<ArtifactViewModel> Artifacts { get; } = new();

    public ObservableCollection<SegmentViewModel> Segments { get; } = new();

    private void RefreshSegments()
    {
        Segments.Clear();
        foreach (var segment in MessageContentParser.Parse(_text))
            Segments.Add(new SegmentViewModel(segment));
    }
}
