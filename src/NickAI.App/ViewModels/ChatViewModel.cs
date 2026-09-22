using System.Collections.ObjectModel;
using System.Text;
using Microsoft.Win32;
using NickAI.App.Services;
using NickAI.Core.Abstractions;
using NickAI.Core.Models;
using NickAI.Core.Services;

namespace NickAI.App.ViewModels;

public sealed class ChatViewModel : ObservableObject
{
    private readonly ChatService _chatService;
    private readonly SessionState _session;
    private readonly ShellService _shell;

    private Conversation _conversation = new();
    private string _input = string.Empty;
    private bool _isBusy;
    private string _status = "Ready";
    private CancellationTokenSource? _cts;

    public ChatViewModel(ChatService chatService, SessionState session, ShellService shell)
    {
        _chatService = chatService;
        _session = session;
        _shell = shell;

        _session.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SessionState.ActiveProjectLabel))
                OnPropertyChanged(nameof(ActiveProjectLabel));
        };

        SendCommand = new AsyncRelayCommand(SendAsync, () => CanSend);
        StopCommand = new RelayCommand(Stop, () => IsBusy);
        NewChatCommand = new RelayCommand(NewChat);
        UsePromptCommand = new RelayCommand(parameter => { if (parameter is string s) Input = s; });
        AttachCommand = new RelayCommand(Attach);
        OpenConversationCommand = new RelayCommand(parameter => { if (parameter is Conversation c) OpenConversation(c); });

        Conversations.Add(_conversation);
    }

    public ObservableCollection<MessageViewModel> Messages { get; } = new();

    public ObservableCollection<Conversation> Conversations { get; } = new();

    public IReadOnlyList<string> SamplePrompts { get; } = new[]
    {
        "Create a C# console app that renames files in a folder.",
        "Write a Python script that summarises a CSV file.",
        "Build my project into an EXE.",
        "Explain what a WebSocket is in three sentences.",
    };

    public AsyncRelayCommand SendCommand { get; }

    public RelayCommand StopCommand { get; }

    public RelayCommand NewChatCommand { get; }

    public RelayCommand UsePromptCommand { get; }

    public RelayCommand AttachCommand { get; }

    public RelayCommand OpenConversationCommand { get; }

    public string Input
    {
        get => _input;
        set
        {
            if (SetProperty(ref _input, value))
                OnPropertyChanged(nameof(CanSend));
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanSend));
                StopCommand.RaiseCanExecuteChanged();
                SendCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanSend => !IsBusy && !string.IsNullOrWhiteSpace(_input);

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public string ActiveProjectLabel => _session.ActiveProjectLabel;

    public bool HasMessages => Messages.Count > 0;

    private async Task SendAsync()
    {
        var text = Input.Trim();
        if (text.Length == 0 || IsBusy) return;

        Input = string.Empty;

        Messages.Add(new MessageViewModel(ChatRole.User, text));
        OnPropertyChanged(nameof(HasMessages));
        _conversation.Messages.Add(new ChatMessage { Role = ChatRole.User, Content = text });
        if (_conversation.Title == "New chat")
            _conversation.Title = text.Length <= 48 ? text : text[..48] + "...";

        var assistant = new MessageViewModel(ChatRole.Assistant, string.Empty) { IsStreaming = true };
        Messages.Add(assistant);

        _cts = new CancellationTokenSource();
        IsBusy = true;
        Status = "Planning...";

        var builder = new StringBuilder();

        try
        {
            await foreach (var evt in _chatService.SendAsync(_conversation, text, _session.ActiveProject, _cts.Token))
            {
                switch (evt.Kind)
                {
                    case ChatStreamEventKind.Activity:
                        Status = evt.Text ?? string.Empty;
                        assistant.Activity.Add(evt.Text ?? string.Empty);
                        break;

                    case ChatStreamEventKind.Delta:
                        builder.Append(evt.Text);
                        assistant.Text = builder.ToString();
                        Status = "Responding...";
                        break;

                    case ChatStreamEventKind.Artifact:
                        if (evt.Artifact is not null)
                            assistant.Artifacts.Add(new ArtifactViewModel(evt.Artifact, _shell));
                        break;

                    case ChatStreamEventKind.Plan:
                        if (evt.Plan is not null)
                            assistant.PlanSummary = BuildPlanText(evt.Plan);
                        break;

                    case ChatStreamEventKind.Error:
                        assistant.Error = evt.Text;
                        if (builder.Length > 0) builder.AppendLine().AppendLine();
                        builder.Append("⚠ ").Append(evt.Text);
                        assistant.Text = builder.ToString();
                        Status = "Error";
                        break;

                    case ChatStreamEventKind.Completed:
                        Status = "Ready";
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            builder.AppendLine().AppendLine("[stopped]");
            assistant.Text = builder.ToString();
            Status = "Stopped";
        }
        catch (Exception ex)
        {
            assistant.Error = ex.Message;
            Status = "Error";
        }
        finally
        {
            assistant.IsStreaming = false;
            assistant.Text = builder.ToString();
            _conversation.Messages.Add(new ChatMessage { Role = ChatRole.Assistant, Content = assistant.Text });
            _conversation.UpdatedAt = DateTimeOffset.Now;
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void Stop()
    {
        Status = "Stopping...";
        _cts?.Cancel();
    }

    private void NewChat()
    {
        _conversation = new Conversation();
        Conversations.Insert(0, _conversation);
        Messages.Clear();
        OnPropertyChanged(nameof(HasMessages));
        Status = "Ready";
    }

    private void OpenConversation(Conversation conversation)
    {
        _conversation = conversation;
        Messages.Clear();
        foreach (var message in conversation.Messages.Where(m => m.Role is ChatRole.User or ChatRole.Assistant))
            Messages.Add(new MessageViewModel(message.Role, message.Content));
        OnPropertyChanged(nameof(HasMessages));
    }

    private void Attach()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Attach a file",
            CheckFileExists = true,
            Multiselect = false,
        };

        if (dialog.ShowDialog() != true) return;

        var note = $"Use the file at {dialog.FileName} as context.";
        Input = string.IsNullOrWhiteSpace(Input) ? note : Input + Environment.NewLine + note;
    }

    private static string BuildPlanText(Plan plan)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Plan - {plan.Summary}");
        foreach (var step in plan.Steps)
        {
            var mark = step.Status switch
            {
                AgentTaskStatus.Succeeded => "done",
                AgentTaskStatus.Failed => "failed",
                AgentTaskStatus.Cancelled => "cancelled",
                AgentTaskStatus.Running => "running",
                _ => "pending",
            };
            builder.Append("• ").Append(step.Title).Append(" - ").Append(mark);
            if (!string.IsNullOrWhiteSpace(step.Error)) builder.Append(": ").Append(step.Error);
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }
}
