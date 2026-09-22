namespace NickAI.Core.Models;

/// <summary>Capabilities a model may or may not actually support.</summary>
[Flags]
public enum ModelCapabilities
{
    None = 0,
    Completion = 1,
    Tools = 2,
    Vision = 4,
    Embedding = 8,
    Speech = 16,
}

/// <summary>A model reported by a provider (e.g. an Ollama install).</summary>
public sealed class ModelInfo
{
    public required string Name { get; init; }

    public string Provider { get; init; } = "ollama";

    public string? Family { get; set; }

    public string? ParameterSize { get; set; }

    public string? Quantization { get; set; }

    public long SizeBytes { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public ModelCapabilities Capabilities { get; set; } = ModelCapabilities.Completion;

    public bool Supports(ModelCapabilities capability) => Capabilities.HasFlag(capability);

    public string SizeDisplay => SizeBytes <= 0
        ? "unknown"
        : $"{SizeBytes / 1024d / 1024d / 1024d:0.0} GB";
}
