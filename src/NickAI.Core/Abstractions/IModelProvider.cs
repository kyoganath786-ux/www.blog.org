using NickAI.Core.Models;

namespace NickAI.Core.Abstractions;

/// <summary>
/// A source of language/vision/embedding models. Implementations must never
/// advertise a capability a model does not actually support.
/// </summary>
public interface IModelProvider
{
    /// <summary>Stable provider key, e.g. "ollama".</summary>
    string Name { get; }

    /// <summary>True when the provider runtime is reachable.</summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>Installed models with honest capability flags.</summary>
    Task<IReadOnlyList<ModelInfo>> ListModelsAsync(CancellationToken ct = default);

    /// <summary>Streams content deltas as they arrive.</summary>
    IAsyncEnumerable<string> StreamChatAsync(ModelRequest request, CancellationToken ct = default);

    /// <summary>Runs a completion to the end and returns the full text.</summary>
    Task<ModelResult> CompleteAsync(ModelRequest request, CancellationToken ct = default);
}
