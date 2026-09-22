using System.Runtime.CompilerServices;
using NickAI.Core.Abstractions;
using NickAI.Core.Models;

namespace NickAI.Core.Tests.Fakes;

/// <summary>A provider whose reachability and model list are controlled by the test.</summary>
internal sealed class FakeModelProvider : IModelProvider
{
    private readonly List<ModelInfo> _models;

    public FakeModelProvider(string name, bool available, params ModelInfo[] models)
    {
        Name = name;
        Available = available;
        _models = models.ToList();
    }

    public string Name { get; }

    /// <summary>Simulates an Ollama process that is not running.</summary>
    public bool Available { get; set; }

    /// <summary>Simulates a provider that fails while listing models.</summary>
    public bool ThrowOnList { get; set; }

    public Task<bool> IsAvailableAsync(CancellationToken ct = default) => Task.FromResult(Available);

    public Task<IReadOnlyList<ModelInfo>> ListModelsAsync(CancellationToken ct = default)
    {
        if (ThrowOnList) throw new InvalidOperationException("provider unreachable");
        return Task.FromResult<IReadOnlyList<ModelInfo>>(_models);
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        ModelRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.CompletedTask;
        yield break;
    }

    public Task<ModelResult> CompleteAsync(ModelRequest request, CancellationToken ct = default) =>
        Task.FromResult(new ModelResult(string.Empty, request.Model, TimeSpan.Zero));
}
