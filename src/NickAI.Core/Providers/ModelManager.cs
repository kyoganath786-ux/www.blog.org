using Microsoft.Extensions.Logging;
using NickAI.Core.Abstractions;
using NickAI.Core.Models;

namespace NickAI.Core.Providers;

/// <summary>
/// Owns the registered providers, the aggregated model list and the currently
/// selected model. Raising <see cref="Changed"/> lets the UI refresh reactively.
/// </summary>
public sealed class ModelManager
{
    private readonly Dictionary<string, IModelProvider> _providers = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ModelInfo> _models = new();
    private readonly ILogger<ModelManager>? _logger;

    public ModelManager(ILogger<ModelManager>? logger = null) => _logger = logger;

    public IReadOnlyList<ModelInfo> Models => _models;

    public IReadOnlyCollection<IModelProvider> Providers => _providers.Values;

    public string? ActiveModel { get; private set; }

    public event EventHandler? Changed;

    public void Register(IModelProvider provider) => _providers[provider.Name] = provider;

    public IModelProvider? ActiveProvider =>
        ActiveModel is null ? _providers.Values.FirstOrDefault() : ProviderFor(ActiveModel);

    public IModelProvider? ProviderFor(string model)
    {
        var match = _models.FirstOrDefault(m => string.Equals(m.Name, model, StringComparison.OrdinalIgnoreCase));
        if (match is not null && _providers.TryGetValue(match.Provider, out var provider))
            return provider;
        return _providers.Values.FirstOrDefault();
    }

    /// <summary>Polls every provider and rebuilds the model list.</summary>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        _models.Clear();
        foreach (var provider in _providers.Values)
        {
            try
            {
                if (!await provider.IsAvailableAsync(ct).ConfigureAwait(false))
                {
                    _logger?.LogInformation("Provider {Provider} is not available.", provider.Name);
                    continue;
                }

                _models.AddRange(await provider.ListModelsAsync(ct).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to refresh provider {Provider}.", provider.Name);
            }
        }

        if (ActiveModel is null || !_models.Any(m => string.Equals(m.Name, ActiveModel, StringComparison.OrdinalIgnoreCase)))
        {
            ActiveModel = _models.FirstOrDefault()?.Name;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SelectModel(string model)
    {
        if (string.IsNullOrWhiteSpace(model)) return;
        ActiveModel = model;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
