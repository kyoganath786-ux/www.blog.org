using NickAI.App.Services;
using NickAI.Core.Models;

namespace NickAI.App.ViewModels;

public sealed class ArtifactViewModel : ObservableObject
{
    private readonly ShellService _shell;

    public ArtifactViewModel(Artifact artifact, ShellService shell)
    {
        Model = artifact;
        _shell = shell;
        OpenCommand = new RelayCommand(() => _shell.OpenFile(Model.FilePath), () => Model.HasFile);
        OpenFolderCommand = new RelayCommand(() => _shell.RevealInExplorer(Model.FilePath), () => Model.HasFile);
    }

    public Artifact Model { get; }

    public string Name => Model.Name;

    public string TypeLabel => Model.Type.ToString().ToUpperInvariant();

    public string? Preview => Model.Preview;

    public string? Path => Model.FilePath;

    public bool HasFile => Model.HasFile;

    public string CreatedLabel => Model.CreatedAt.ToString("t");

    public RelayCommand OpenCommand { get; }

    public RelayCommand OpenFolderCommand { get; }
}
