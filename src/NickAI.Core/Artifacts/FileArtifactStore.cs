using NickAI.Core.Abstractions;
using NickAI.Core.Models;

namespace NickAI.Core.Artifacts;

/// <summary>Stores artifacts as real files under a root folder.</summary>
public sealed class FileArtifactStore : IArtifactStore
{
    private readonly List<Artifact> _artifacts = new();
    private readonly object _gate = new();

    public FileArtifactStore(string rootPath)
    {
        RootPath = rootPath;
        Directory.CreateDirectory(RootPath);
    }

    public string RootPath { get; }

    public IReadOnlyCollection<Artifact> All
    {
        get { lock (_gate) return _artifacts.ToList(); }
    }

    public event EventHandler<Artifact>? ArtifactAdded;

    public string Add(Artifact artifact)
    {
        lock (_gate)
        {
            _artifacts.Add(artifact);
        }
        ArtifactAdded?.Invoke(this, artifact);
        return artifact.Id;
    }

    public Artifact CreateFile(
        string name,
        ArtifactType type,
        string content,
        string? projectId = null,
        IDictionary<string, string>? metadata = null)
    {
        Artifact artifact;
        lock (_gate)
        {
            // Reserving and writing under the same lock keeps two writers from
            // claiming the same free name and overwriting each other's content.
            var path = ReservePath(name);
            File.WriteAllText(path, content);

            artifact = new Artifact
            {
                Name = Path.GetFileName(path),
                Type = type,
                FilePath = path,
                MimeType = MimeFor(type),
                ProjectId = projectId,
            };

            if (metadata is not null)
            {
                foreach (var (key, value) in metadata) artifact.Metadata[key] = value;
            }
        }

        Add(artifact);
        return artifact;
    }

    public Artifact? Get(string id)
    {
        lock (_gate)
        {
            return _artifacts.FirstOrDefault(a => a.Id == id);
        }
    }

    public string ReservePath(string fileName)
    {
        Directory.CreateDirectory(RootPath);
        var safe = SanitizeFileName(fileName);
        var candidate = Path.Combine(RootPath, safe);
        if (!File.Exists(candidate)) return candidate;

        var stem = Path.GetFileNameWithoutExtension(safe);
        var ext = Path.GetExtension(safe);
        for (var i = 2; i < 10_000; i++)
        {
            candidate = Path.Combine(RootPath, $"{stem}-{i}{ext}");
            if (!File.Exists(candidate)) return candidate;
        }

        return Path.Combine(RootPath, $"{stem}-{Guid.NewGuid():n}{ext}");
    }

    public bool Remove(string id)
    {
        Artifact? artifact;
        lock (_gate)
        {
            artifact = _artifacts.FirstOrDefault(a => a.Id == id);
            if (artifact is null) return false;
            _artifacts.Remove(artifact);
        }

        try
        {
            if (artifact.HasFile && artifact.FilePath is not null)
                File.Delete(artifact.FilePath);
        }
        catch (IOException)
        {
            // File locked; leave it on disk.
        }

        return true;
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        foreach (var invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');
        return string.IsNullOrWhiteSpace(name) ? $"artifact-{Guid.NewGuid():n}" : name;
    }

    private static string MimeFor(ArtifactType type) => type switch
    {
        ArtifactType.Code => "text/plain",
        ArtifactType.Text => "text/plain",
        ArtifactType.Markdown => "text/markdown",
        ArtifactType.Csv => "text/csv",
        ArtifactType.Image => "image/png",
        ArtifactType.Video => "video/mp4",
        ArtifactType.Audio => "audio/mpeg",
        ArtifactType.Pdf => "application/pdf",
        ArtifactType.Docx => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ArtifactType.Pptx => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        ArtifactType.Xlsx => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ArtifactType.Zip => "application/zip",
        ArtifactType.Exe => "application/vnd.microsoft.portable-executable",
        _ => "application/octet-stream",
    };
}
