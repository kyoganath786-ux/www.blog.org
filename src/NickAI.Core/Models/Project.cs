namespace NickAI.Core.Models;

/// <summary>Project-specific long-term memory, never uploaded silently.</summary>
public sealed class ProjectMemory
{
    public List<string> Requirements { get; set; } = new();
    public List<string> Architecture { get; set; } = new();
    public List<string> Technology { get; set; } = new();
    public List<string> ImportantFiles { get; set; } = new();
    public List<string> BuildCommands { get; set; } = new();
    public List<string> TestCommands { get; set; } = new();
    public List<string> KnownErrors { get; set; } = new();
    public List<string> CompletedFeatures { get; set; } = new();
    public List<string> DesignDecisions { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}

public enum ProjectKind
{
    Unknown,
    DotNet,
    Node,
    Python,
    Rust,
    Go,
    Flutter,
    Unity,
    Godot,
    Web,
    Generic,
}

/// <summary>A workspace the user is building inside NICK AI.</summary>
public sealed class Project
{
    public string Id { get; init; } = Guid.NewGuid().ToString("n");

    public required string Name { get; set; }

    public string? RootPath { get; set; }

    public ProjectKind Kind { get; set; } = ProjectKind.Unknown;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;

    public ProjectMemory Memory { get; set; } = new();

    public List<string> ArtifactIds { get; set; } = new();

    /// <summary>Build/publish command detected or configured for this project.</summary>
    public string? BuildCommand { get; set; }

    public string? TestCommand { get; set; }
}
