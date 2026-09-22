using System.Text.Json;
using NickAI.Core.Abstractions;
using NickAI.Core.Models;

namespace NickAI.Core.Projects;

/// <summary>Persists projects to a JSON file and detects project technology.</summary>
public sealed class FileProjectStore : IProjectStore
{
    private readonly List<Project> _projects = new();
    private readonly string _indexPath;
    private readonly object _gate = new();

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public FileProjectStore(string baseDirectory)
    {
        Directory.CreateDirectory(baseDirectory);
        _indexPath = Path.Combine(baseDirectory, "projects.json");
        Load();
    }

    public IReadOnlyCollection<Project> All
    {
        get { lock (_gate) return _projects.ToList(); }
    }

    public event EventHandler<Project>? ProjectCreated;

    public Project Create(string name, string? rootPath = null, ProjectKind kind = ProjectKind.Unknown)
    {
        var project = new Project
        {
            Name = name,
            RootPath = rootPath,
            Kind = kind != ProjectKind.Unknown
                ? kind
                : !string.IsNullOrWhiteSpace(rootPath) && Directory.Exists(rootPath)
                    ? DetectKind(rootPath)
                    : ProjectKind.Unknown,
        };

        lock (_gate)
        {
            _projects.Add(project);
        }

        Save(project);
        ProjectCreated?.Invoke(this, project);
        return project;
    }

    public Project? Get(string id)
    {
        lock (_gate)
        {
            return _projects.FirstOrDefault(p => p.Id == id);
        }
    }

    public void Save(Project project)
    {
        lock (_gate)
        {
            // Match on id, not reference, so a rehydrated copy of a project
            // replaces the stored one instead of being appended as a duplicate.
            var index = _projects.FindIndex(p => p.Id == project.Id);
            if (index >= 0) _projects[index] = project;
            else _projects.Add(project);

            Write();
        }
    }

    public bool Delete(string id)
    {
        lock (_gate)
        {
            var project = _projects.FirstOrDefault(p => p.Id == id);
            if (project is null) return false;
            _projects.Remove(project);
            Write();
            return true;
        }
    }

    public ProjectKind DetectKind(string rootPath)
    {
        if (!Directory.Exists(rootPath)) return ProjectKind.Unknown;

        if (File.Exists(Path.Combine(rootPath, "project.godot"))) return ProjectKind.Godot;
        if (Directory.Exists(Path.Combine(rootPath, "ProjectSettings"))) return ProjectKind.Unity;
        if (HasFile(rootPath, "*.sln") || HasFile(rootPath, "*.csproj")) return ProjectKind.DotNet;
        if (File.Exists(Path.Combine(rootPath, "package.json"))) return ProjectKind.Node;
        if (File.Exists(Path.Combine(rootPath, "pyproject.toml")) || File.Exists(Path.Combine(rootPath, "requirements.txt"))) return ProjectKind.Python;
        if (File.Exists(Path.Combine(rootPath, "Cargo.toml"))) return ProjectKind.Rust;
        if (File.Exists(Path.Combine(rootPath, "go.mod"))) return ProjectKind.Go;
        if (File.Exists(Path.Combine(rootPath, "pubspec.yaml"))) return ProjectKind.Flutter;
        if (File.Exists(Path.Combine(rootPath, "index.html"))) return ProjectKind.Web;
        if (HasFile(rootPath, "*.cs")) return ProjectKind.DotNet;

        return ProjectKind.Generic;
    }

    private static bool HasFile(string directory, string pattern)
    {
        try
        {
            return Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly).Any();
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void Load()
    {
        if (!File.Exists(_indexPath)) return;
        try
        {
            var json = File.ReadAllText(_indexPath);
            var loaded = JsonSerializer.Deserialize<List<Project>>(json, JsonOptions);
            if (loaded is not null) _projects.AddRange(loaded);
        }
        catch (Exception)
        {
            // Corrupt index; start fresh rather than crash the app.
        }
    }

    private void Write()
    {
        try
        {
            File.WriteAllText(_indexPath, JsonSerializer.Serialize(_projects, JsonOptions));
        }
        catch (Exception)
        {
            // Best effort persistence.
        }
    }
}
