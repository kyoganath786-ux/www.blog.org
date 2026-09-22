using System.Collections.ObjectModel;
using NickAI.App.Services;
using NickAI.Core.Providers;

namespace NickAI.App.ViewModels;

public sealed record NavItem(string Key, string Title, string Glyph);

/// <summary>Top-level shell: navigation, active model and theme.</summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly ChatViewModel _chat;
    private readonly ArtifactsViewModel _artifacts;
    private readonly ModelsViewModel _modelsView;
    private readonly PermissionsViewModel _permissions;
    private readonly ProjectsViewModel _projects;
    private readonly ModelManager _models;

    private object _currentView;
    private NavItem _selectedNav;
    private string? _selectedModel;
    private ThemeMode _selectedTheme = ThemeMode.Dark;

    public MainViewModel(
        ChatViewModel chat,
        ProjectsViewModel projects,
        ArtifactsViewModel artifacts,
        ModelsViewModel modelsView,
        PermissionsViewModel permissions,
        ModelManager models)
    {
        _chat = chat;
        _projects = projects;
        _artifacts = artifacts;
        _modelsView = modelsView;
        _permissions = permissions;
        _models = models;

        NavItems = new ObservableCollection<NavItem>
        {
            new("chat", "Chat", "\U0001F4AC"),
            new("projects", "Projects", "\U0001F4C1"),
            new("artifacts", "Artifacts", "\U0001F4E6"),
            new("models", "Models", "\U0001F9E0"),
            new("permissions", "Permissions", "\U0001F6E1"),
        };

        _currentView = _chat;
        _selectedNav = NavItems[0];
        _selectedModel = _models.ActiveModel;

        NewChatCommand = chat.NewChatCommand;
        RefreshModelsCommand = new AsyncRelayCommand(() => _modelsView.RefreshAsync());
        OpenSettingsCommand = new RelayCommand(() => SelectedNav = NavItems.First(n => n.Key == "permissions"));

        _models.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(ModelOptions));
            SelectedModel = _models.ActiveModel;
        };

        _chat.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ChatViewModel.Status)) OnPropertyChanged(nameof(StatusText));
            if (e.PropertyName == nameof(ChatViewModel.ActiveProjectLabel)) OnPropertyChanged(nameof(ActiveProjectLabel));
        };
    }

    public ObservableCollection<NavItem> NavItems { get; }

    public RelayCommand NewChatCommand { get; }

    public AsyncRelayCommand RefreshModelsCommand { get; }

    public RelayCommand OpenSettingsCommand { get; }

    public object CurrentView
    {
        get => _currentView;
        private set => SetProperty(ref _currentView, value);
    }

    public NavItem SelectedNav
    {
        get => _selectedNav;
        set
        {
            if (!SetProperty(ref _selectedNav, value)) return;
            CurrentView = value.Key switch
            {
                "projects" => _projects,
                "artifacts" => _artifacts,
                "models" => _modelsView,
                "permissions" => _permissions,
                _ => _chat,
            };
        }
    }

    public string StatusText => _chat.Status;

    public string ActiveProjectLabel => _chat.ActiveProjectLabel;

    public IReadOnlyList<string> ModelOptions => _models.Models.Select(m => m.Name).ToList();

    public string? SelectedModel
    {
        get => _selectedModel;
        set
        {
            if (!SetProperty(ref _selectedModel, value)) return;
            if (!string.IsNullOrWhiteSpace(value)) _models.SelectModel(value);
        }
    }

    public IReadOnlyList<ThemeMode> ThemeOptions { get; } = new[] { ThemeMode.Dark, ThemeMode.Light, ThemeMode.System };

    public ThemeMode SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (!SetProperty(ref _selectedTheme, value)) return;
            ThemeManager.Apply(value);
        }
    }

    /// <summary>Called once after the window is shown.</summary>
    public Task InitializeAsync() => _modelsView.RefreshAsync();
}
