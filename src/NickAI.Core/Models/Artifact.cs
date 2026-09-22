namespace NickAI.Core.Models;

/// <summary>Every generated output is represented as an artifact.</summary>
public enum ArtifactType
{
    Text,
    Code,
    Image,
    Video,
    Audio,
    Pdf,
    Docx,
    Pptx,
    Xlsx,
    Csv,
    Markdown,
    Zip,
    Exe,
    Installer,
    Website,
    Game,
    Application,
    Chart,
    Dataset,
    Report,
}

/// <summary>A generated or imported deliverable on disk.</summary>
public sealed class Artifact
{
    public string Id { get; init; } = Guid.NewGuid().ToString("n");

    public required string Name { get; set; }

    public ArtifactType Type { get; init; }

    public string? FilePath { get; set; }

    public string? MimeType { get; set; }

    /// <summary>Short inline preview text (truncated file content, dimensions, etc.).</summary>
    public string? Preview { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;

    public string? ProjectId { get; set; }

    public Dictionary<string, string> Metadata { get; } = new();

    /// <summary>True when the artifact is backed by a file that actually exists.</summary>
    public bool HasFile => !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath);
}
