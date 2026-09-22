using NickAI.Core.Abstractions;
using NickAI.Core.Models;

namespace NickAI.Core.Tests.Fakes;

/// <summary>In-memory artifact store; never touches the disk.</summary>
internal sealed class FakeArtifactStore : IArtifactStore
{
    private readonly List<Artifact> _artifacts = new();

    public string RootPath { get; init; } = Path.GetTempPath();

    public IReadOnlyCollection<Artifact> All => _artifacts;

    public event EventHandler<Artifact>? ArtifactAdded;

    public string Add(Artifact artifact)
    {
        _artifacts.Add(artifact);
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
        var artifact = new Artifact
        {
            Name = name,
            Type = type,
            Preview = content,
            ProjectId = projectId,
        };

        if (metadata is not null)
        {
            foreach (var pair in metadata) artifact.Metadata[pair.Key] = pair.Value;
        }

        Add(artifact);
        return artifact;
    }

    public Artifact? Get(string id) => _artifacts.FirstOrDefault(a => a.Id == id);

    public string ReservePath(string fileName) => Path.Combine(RootPath, fileName);

    public bool Remove(string id) => _artifacts.RemoveAll(a => a.Id == id) > 0;
}
