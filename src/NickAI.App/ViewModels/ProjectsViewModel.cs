using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Win32;
using NickAI.App.Services;
using NickAI.Core.Abstractions;
using NickAI.Core.Models;

namespace NickAI.App.ViewModels;

public sealed class ProjectsViewModel : ObservableObject
{
    private readonly IProjectStore _store;
    private readonly SessionState _session;
    private readonly ShellService _shell;
    private Project? _selected;

    public ProjectsViewModel(IProjectStore store, SessionState session, ShellService shell)
    {
        _store = store;
        _session = session;
        _shell = shell;

        foreach (var project in _store.All)
            Items.Add(project);

        AddProjectCommand = new RelayCommand(AddProject);
        OpenFolderCommand = new RelayCommand(parameter => { if (parameter is Project p) _shell.OpenFolder(p.RootPath); });
        UseProjectCommand = new RelayCommand(parameter => { if (parameter is Project p) Select(p); });
    }

    public ObservableCollection<Project> Items { get; } = new();

    public RelayCommand AddProjectCommand { get; }

    public RelayCommand OpenFolderCommand { get; }

    public RelayCommand UseProjectCommand { get; }

    public Project? Selected
    {
        get => _selected;
        set => SetProperty(ref _selected, value);
    }

    public bool IsEmpty => Items.Count == 0;

    public string ActiveProjectLabel => _session.ActiveProjectLabel;

    private void AddProject()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select a project folder",
            Multiselect = false,
        };

        if (dialog.ShowDialog() != true) return;

        var name = Path.GetFileName(dialog.FolderName.TrimEnd(Path.DirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(name)) name = "Project";

        var project = _store.Create(name, dialog.FolderName);
        Items.Add(project);
        OnPropertyChanged(nameof(IsEmpty));
        Select(project);
    }

    private void Select(Project project)
    {
        Selected = project;
        _session.ActiveProject = project;
        OnPropertyChanged(nameof(ActiveProjectLabel));
    }
}
