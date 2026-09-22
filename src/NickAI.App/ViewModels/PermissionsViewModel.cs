using System.Collections.ObjectModel;
using NickAI.Core.Abstractions;
using NickAI.Core.Models;

namespace NickAI.App.ViewModels;

/// <summary>Permission center: ALLOW / ASK / DENY per category.</summary>
public sealed class PermissionsViewModel : ObservableObject
{
    public PermissionsViewModel(IPermissionService permissions)
    {
        Rows = new ObservableCollection<PermissionRowViewModel>(
            Enum.GetValues<PermissionCategory>().Select(c => new PermissionRowViewModel(permissions, c)));
    }

    public ObservableCollection<PermissionRowViewModel> Rows { get; }
}

public sealed class PermissionRowViewModel : ObservableObject
{
    private readonly IPermissionService _service;
    private PermissionMode _mode;

    public PermissionRowViewModel(IPermissionService service, PermissionCategory category)
    {
        _service = service;
        Category = category;
        _mode = service.ModeFor(category);
    }

    public PermissionCategory Category { get; }

    public string Title => Category.ToString();

    public string Description => Category switch
    {
        PermissionCategory.Browser => "Open pages, navigate and extract content.",
        PermissionCategory.Files => "Read and write files in selected folders.",
        PermissionCategory.Applications => "Launch and focus installed applications.",
        PermissionCategory.Terminal => "Run development commands such as build and test.",
        PermissionCategory.Network => "Make outbound network requests.",
        PermissionCategory.Screen => "Capture the screen for the computer agent.",
        PermissionCategory.Microphone => "Use the microphone for voice input.",
        PermissionCategory.Camera => "Use the camera.",
        PermissionCategory.Downloads => "Download files from the web.",
        PermissionCategory.Uploads => "Upload files to external services.",
        PermissionCategory.Git => "Run local git commands.",
        PermissionCategory.Publishing => "Deploy or publish projects externally.",
        _ => string.Empty,
    };

    public static PermissionMode[] Modes { get; } = Enum.GetValues<PermissionMode>();

    public PermissionMode Mode
    {
        get => _mode;
        set
        {
            if (SetProperty(ref _mode, value))
                _service.SetMode(Category, value);
        }
    }
}
