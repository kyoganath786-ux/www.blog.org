using NickAI.App.ViewModels;
using NickAI.Core.Models;

namespace NickAI.App.Services;

/// <summary>Workspace-wide state shared by view models.</summary>
public sealed class SessionState : ObservableObject
{
    private Project? _activeProject;

    /// <summary>The project new requests are attached to.</summary>
    public Project? ActiveProject
    {
        get => _activeProject;
        set
        {
            if (SetProperty(ref _activeProject, value))
                OnPropertyChanged(nameof(ActiveProjectLabel));
        }
    }

    public string ActiveProjectLabel => _activeProject is null ? "No project" : _activeProject.Name;
}
