using System.Collections.ObjectModel;
using System.Windows;
using NickAI.App.Services;
using NickAI.Core.Abstractions;
using NickAI.Core.Models;

namespace NickAI.App.ViewModels;

public sealed class ArtifactsViewModel : ObservableObject
{
    private readonly IArtifactStore _store;
    private readonly ShellService _shell;

    public ArtifactsViewModel(IArtifactStore store, ShellService shell)
    {
        _store = store;
        _shell = shell;

        foreach (var artifact in _store.All.OrderByDescending(a => a.CreatedAt))
            Items.Add(new ArtifactViewModel(artifact, _shell));

        _store.ArtifactAdded += OnArtifactAdded;
    }

    public ObservableCollection<ArtifactViewModel> Items { get; } = new();

    public bool IsEmpty => Items.Count == 0;

    public string RootPath => _store.RootPath;

    public RelayCommand OpenRootCommand => new(() => _shell.OpenFolder(_store.RootPath));

    private void OnArtifactAdded(object? sender, Artifact artifact)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null) return;

        dispatcher.Invoke(() =>
        {
            Items.Insert(0, new ArtifactViewModel(artifact, _shell));
            OnPropertyChanged(nameof(IsEmpty));
        });
    }
}
