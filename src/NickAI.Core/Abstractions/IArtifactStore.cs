using NickAI.Core.Models;

namespace NickAI.Core.Abstractions;

/// <summary>Stores artifacts and their backing files.</summary>
public interface IArtifactStore
{
    /// <summary>Root folder where artifact files are written.</summary>
    string RootPath { get; }

    IReadOnlyCollection<Artifact> All { get; }

    event EventHandler<Artifact>? ArtifactAdded;

    /// <summary>Registers an artifact and returns its id.</summary>
    string Add(Artifact artifact);

    /// <summary>Creates an artifact backed by a new file and writes the given content.</summary>
    Artifact CreateFile(string name, ArtifactType type, string content, string? projectId = null, IDictionary<string, string>? metadata = null);

    Artifact? Get(string id);

    /// <summary>Absolute path for a new file with the given name inside the artifact root.</summary>
    string ReservePath(string fileName);

    bool Remove(string id);
}
