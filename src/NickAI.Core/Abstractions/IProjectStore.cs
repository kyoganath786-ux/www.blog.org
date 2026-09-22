using NickAI.Core.Models;

namespace NickAI.Core.Abstractions;

/// <summary>Loads and saves projects and their memory.</summary>
public interface IProjectStore
{
    IReadOnlyCollection<Project> All { get; }

    event EventHandler<Project>? ProjectCreated;

    Project Create(string name, string? rootPath = null, ProjectKind kind = ProjectKind.Unknown);

    Project? Get(string id);

    void Save(Project project);

    bool Delete(string id);

    /// <summary>Best-effort detection of a project's technology from its folder contents.</summary>
    ProjectKind DetectKind(string rootPath);
}
